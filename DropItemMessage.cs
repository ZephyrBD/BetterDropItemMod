using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BetterDropItem
{
	public class DropItemMessage : INetMessage
	{
		private NetworkInstanceId netId;
		private PickupIndex pickupIndex;

        public DropItemMessage() {
        }

		public DropItemMessage(NetworkInstanceId netId, PickupIndex pickupIndex)
		{
			this.netId = netId;
			this.pickupIndex = pickupIndex;
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.Write(netId);
			PickupIndex.WriteToNetworkWriter(writer, pickupIndex);
		}

		public void Deserialize(NetworkReader reader)
		{
			netId = reader.ReadNetworkId();
			pickupIndex = reader.ReadPickupIndex();
		}

		public override string ToString() => $"DropItemMessage: {pickupIndex}";

		private void Log(string message) {
			BetterDropItemPlugin.Logger.LogDebug(message);
		}

        public void OnReceived() {
			if (!NetworkServer.active) {
				return;
            }

			Log("Received drop item message");
			Log("NetworkID" + netId.ToString());
            Log("PickupIndex" + this.pickupIndex.ToString());

			var bodyObject = Util.FindNetworkObject(netId);
			if (!bodyObject) return;

			var body = bodyObject.GetComponent<CharacterBody>();
			Log("Body is null: " + (body == null).ToString());
			if (!body) return;

			var inventory = body.master.inventory;

            if (DropItemHandler.DropItem(body, inventory, pickupIndex)
                && BetterDropItemPlugin.enableNotifications.Value)
            {
                DropItemHandler.CreateNotification(body, pickupIndex);
            }
		}
    }
}