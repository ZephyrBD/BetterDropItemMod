using System;
using JetBrains.Annotations;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.UIElements;

namespace BetterDropItem
{
	public class DropItemHandler : MonoBehaviour, IPointerClickHandler
	{
        private Func<CharacterMaster> getMaster;
        private Func<PickupIndex> getPickupIndex;

        public void SetData(Func<CharacterMaster> getMaster, Func<PickupIndex> getPickupIndex) {
            this.getMaster = getMaster;
            this.getPickupIndex = getPickupIndex;
        }

		public void OnPointerClick(PointerEventData eventData)
		{
			BetterDropItemPlugin.Logger.LogDebug("BDI: Pointer click, trying to send network message");
			var master = getMaster();

            BetterDropItemPlugin.Logger.LogDebug("1");
            if (!master || !master.inventory || !master.inventory.hasAuthority)
			{
				return;
			}

            BetterDropItemPlugin.Logger.LogDebug("2");
            var pickupIndex = getPickupIndex();
            BetterDropItemPlugin.Logger.LogDebug("2.1");
            var masterBody = master.GetBody();
            BetterDropItemPlugin.Logger.LogDebug("2.2");
            if (!masterBody)
            {
                BetterDropItemPlugin.Logger.LogDebug("2.3");
                return;
            }

            BetterDropItemPlugin.Logger.LogDebug("3");
            var identity = masterBody.gameObject.GetComponent<NetworkIdentity>();

            BetterDropItemPlugin.Logger.LogDebug("4");
            if (!VerifyIsDroppable(pickupIndex, master.inventory)) return;

			BetterDropItemPlugin.Logger.LogDebug("BDI: Sending network message");

            DropItemMessage itemDropMessage = new DropItemMessage(identity.netId, pickupIndex);
			itemDropMessage.Send(NetworkDestination.Server);
		}

		public static bool DropItem(Vector3 position, Vector3 direction, Inventory inventory, PickupIndex pickupIndex)
		{
			//Verify Tier on the server so that clients can't change their config at-will.
			if (!VerifyIsDroppable(pickupIndex, inventory)) return false;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null) return false;

            BetterDropItemPlugin.Logger.LogDebug("Position: " + position);
            BetterDropItemPlugin.Logger.LogDebug("Direction: " + direction);
            BetterDropItemPlugin.Logger.LogDebug("Inventory: " + inventory.name);
			BetterDropItemPlugin.Logger.LogDebug("Pickup Index: " + pickupIndex);


			if (pickupDef.equipmentIndex != EquipmentIndex.None)
			{
				if (inventory.GetEquipmentIndex() != pickupDef.equipmentIndex)
				{
					return false;
				}
				inventory.SetEquipmentIndex(EquipmentIndex.None, true);
			}
			else
			{
				if (inventory.GetItemCountPermanent(pickupDef.itemIndex) <= 0) 
				{
					return false;
				}

				inventory.RemoveItemPermanent(pickupDef.itemIndex, 1);
			}

			var pickupInfo = new GenericPickupController.CreatePickupInfo()
			{
				pickupIndex = pickupIndex,
				position = position,
            };

            //CreatePickupInfo is a struct, so this has to happen before it is passed along.
            if (BetterDropItemPlugin.commandArtifact.Value
                && pickupDef.equipmentIndex == EquipmentIndex.None
                && RunArtifactManager.instance != null
                && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command))
            {
                pickupInfo.artifactFlag = GenericPickupController.PickupArtifactFlag.COMMAND;
            }

			CreatePickupDroplet(pickupInfo, Vector3.up * 20f + direction * 10f);

            return true;
        }

        //Used by the chat commands, which only have a body to work with.
        public static bool DropItem(CharacterBody body, Inventory inventory, PickupIndex pickupIndex)
        {
            if (!body) return false;

            Vector3 position = body.corePosition;

            Vector3 direction = body.characterDirection ? body.characterDirection.forward : body.transform.forward;

            //Try get aimray
            if (body.inputBank)
            {
                Ray aimRay = body.inputBank.GetAimRay();
                direction = aimRay.direction;
                direction.y = 0f;
                direction.Normalize();
            }

            return DropItem(position, direction, inventory, pickupIndex);
        }

		//Based off of RoR2.PickupDropletController.CreatePickupDroplet
		//The Command artifact flag is opt-in, see "Command Artifact Support (Server-Side)".
        private static void CreatePickupDroplet(GenericPickupController.CreatePickupInfo pickupInfo, Vector3 velocity)
		{
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(PickupDropletController.pickupDropletPrefab, pickupInfo.position, Quaternion.identity);
            PickupDropletController component = gameObject.GetComponent<PickupDropletController>();
            if (component)
            {
                component.createPickupInfo = pickupInfo;
                component.NetworkpickupState = pickupInfo.pickup;
            }
            Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
            component2.velocity = velocity;
            component2.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);

            //This does nothing on the droplet, but will be auto-added to the pickup when it spawns, using a hook.
            if (BetterDropItemPlugin.preventRecycle.Value) gameObject.AddComponent<MarkNonRecyclableComponent>();

            NetworkServer.Spawn(gameObject);
        }


        private static bool VerifyIsDroppable(PickupIndex pickupIndex, Inventory inventory)
        {
			if (!inventory) return false;
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null) return false;

            //Equipments are always droppable
            if (pickupDef.equipmentIndex != EquipmentIndex.None) return true;

			//Check if the inventory has any non-temp items
			if (pickupDef.itemIndex != ItemIndex.None)
			{
				if (inventory.GetItemCountPermanent(pickupDef.itemIndex) <= 0) return false;
			}

            //NoTier pickups (internal/junk items) are never droppable.
            if (pickupDef.itemTier == ItemTier.NoTier) return false;

			if (BetterDropItemPlugin.preventPrayerBeadsAbuse.Value
				&& pickupDef.itemIndex != ItemIndex.None
				&& ItemCatalog.GetItemDef(pickupDef.itemIndex) == DLC2Content.Items.ExtraStatsOnLevelUp)
			{
				return false;
			}

            if (!BetterDropItemPlugin.allowInBazaar.Value)
            {
                SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
                if (currentScene && currentScene.baseSceneName == "bazaar")
                {
                    return false;
                }
            }

            //Unknown tiers (modded items without an ItemTierDef) are allowed, like the original mod.
            ItemTierDef tier = ItemTierCatalog.GetItemTierDef(pickupDef.itemTier);
            bool canDrop = tier == null || tier.isDroppable;

            if (tier != null)
            {
                if (tier.tier == ItemTier.Lunar)
                {
                    //Always allow Lunar Equipment to be dropped
                    canDrop = BetterDropItemPlugin.allowDropLunar.Value || pickupDef.equipmentIndex != EquipmentIndex.None;
                }
                else
                {
                    bool isVoid = tier.tier == ItemTier.VoidBoss
                        || tier.tier == ItemTier.VoidTier1
                        || tier.tier == ItemTier.VoidTier2
                        || tier.tier == ItemTier.VoidTier3;

                    if (isVoid)
                    {
                        canDrop = BetterDropItemPlugin.allowDropVoid.Value;
                    }
                }
            }

            return canDrop;
        }

        public static void CreateNotification(CharacterBody character, PickupIndex pickupIndex)
		{
            if (!character) return;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null) return;

			if (pickupDef.equipmentIndex != EquipmentIndex.None)
			{
				CreateNotification(character, character.transform, pickupDef.equipmentIndex);
			} 
			else
			{
				CreateNotification(character, character.transform, pickupDef.itemIndex);
			}
		}

		private static void CreateNotification(CharacterBody character, Transform transform, EquipmentIndex equipmentIndex)
		{
			var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
			const string title = "Equipment dropped";
			var description = Language.GetString(equipmentDef.nameToken);
			var texture = equipmentDef.pickupIconTexture;

			CreateNotification(character, transform, title, description, texture);
		}

		private static void CreateNotification(CharacterBody character, Transform transform, ItemIndex itemIndex)
		{
			var itemDef = ItemCatalog.GetItemDef(itemIndex);
			const string title = "Item dropped";
			var description = Language.GetString(itemDef.nameToken);
			var texture = itemDef.pickupIconTexture;

			CreateNotification(character, transform, title, description, texture);
		}

		private static void CreateNotification(CharacterBody character, Transform transform, string title, string description, Texture texture)
        {
            var notification = character.gameObject.GetComponent<DropItemNotification>();
            if (notification == null)
            {
                notification = character.gameObject.AddComponent<DropItemNotification>();
                notification.transform.SetParent(transform);
			}
            notification.SetNotification(title, description, texture);
        }
	}
}