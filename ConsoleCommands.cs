using RoR2;
using System;

namespace BetterDropItem
{
	//Port of the original mod's console commands, adapted to the current game API.
	//R2API's CommandHelper scans static methods for [ConCommand] (public or non-public).
	public static class ConsoleCommands
	{
		[ConCommand(commandName = "drop_item", flags = ConVarFlags.ExecuteOnServer, helpText = "Drops an item from your inventory")]
		private static void DropItemCommand(ConCommandArgs args)
		{
			if (args.Count < 1)
			{
				Log("Usage: drop_item <item> [count]");
				return;
			}

			var itemIndex = ItemNameToIndex(args.GetArgString(0));
			if (itemIndex == ItemIndex.None)
			{
				Log("Can't find item specified");
				return;
			}

			var count = args.TryGetArgInt(1) ?? 1;
			if (count <= 0) return;

			var master = args.GetSenderMaster();
			if (!master || !master.inventory) return;

			var body = master.GetBody();
			if (!body) return;

			//Clamp the loop: a client can send any number here, so never let it spin on the server.
			count = Math.Min(count, master.inventory.GetItemCountPermanent(itemIndex));
			count = Math.Min(count, 100);
			if (count <= 0)
			{
				Log("You don't have that item");
				return;
			}

			BetterDropItemPlugin.Logger.LogDebug("Item index: " + itemIndex);

			var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);

			for (int i = 0; i < count; i++)
				DropItemHandler.DropItem(body, master.inventory, pickupIndex);
		}

		[ConCommand(commandName = "drop_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Drops your current equipment")]
		private static void DropEquipCommand(ConCommandArgs args)
		{
			var master = args.GetSenderMaster();
			if (!master || !master.inventory) return;

			var body = master.GetBody();
			if (!body) return;

			var equipmentIndex = master.inventory.GetEquipmentIndex();
			if (equipmentIndex == EquipmentIndex.None)
			{
				Log("No equipment to drop");
				return;
			}

			DropItemHandler.DropItem(body, master.inventory, PickupCatalog.FindPickupIndex(equipmentIndex));
		}

		public static ItemIndex ItemNameToIndex(string name)
		{
			if (string.IsNullOrEmpty(name)) return ItemIndex.None;

			if (Enum.TryParse(name, true, out ItemIndex foundItem) && ItemCatalog.IsIndexValid(foundItem))
			{
				return foundItem;
			}

			foreach (var itemIndex in ItemCatalog.allItems)
			{
				var item = ItemCatalog.GetItemDef(itemIndex);
				if (item == null || string.IsNullOrEmpty(item.name)) continue;

				if (item.name.ToUpper().Contains(name.ToUpper()))
				{
					return item.itemIndex;
				}
			}
			return ItemIndex.None;
		}

		private static void Log(string message)
		{
			BetterDropItemPlugin.Logger.LogInfo(message);
			RoR2Application.Print(message);
		}
	}
}
