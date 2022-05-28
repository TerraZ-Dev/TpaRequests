using System.Collections.Generic;
using System.Linq;

using Terraria;
using TerrariaApi.Server;
using TShockAPI;

using RequestsManagerAPI;

namespace TpaRequests
{
    [ApiVersion(2, 1)]
    public class TpaRequestsPlugin : TerrariaPlugin
    {
        public override string Author => "Zoom L1";
        public override string Name => "Tpa Requests Plugin";

        public TpaRequestsPlugin(Main game) : base(game) { }
        public bool[] AutoDeny = new bool[Main.maxNetPlayers];

        public override void Initialize()
        {
            RequestsManager.AddConfiguration("tp", new RequestConfiguration(true, false, true, true, 10));

            Commands.ChatCommands.Add(new Command("tparequest", TpaRequestCommand, "tpa"));
            Commands.ChatCommands.Add(new Command("tparequest", TpAutoDenyCommand, "tpautodeny", "tpdeny"));
        }

        public async void TpaRequestCommand(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Parameters.Count < 1");
                return;
            }
            
            string plrName = string.Join(" ", args.Parameters);
            var players = TSPlayer.FindByNameOrID(plrName);
            
            if (players.Count > 1)
            {
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                return;
            }
            if (players.Count == 0)
            {
                args.Player.SendErrorMessage("players.Count == 0");
                return;
            }

            var player = players[0];

            (Decision Decision, ICondition BrokenCondition) =
                    await RequestsManager.GetDecision(player, args.Player, "tp",
                    new Messages(null, null, new Dictionary<MessageType, Message>()
                    {
                        [MessageType.AnnounceOutbox] = new Message($"A request for teleportation to a {player.Name} has been sent."),
                        [MessageType.AnnounceInbox] = new Message($"{args.Player.Name} requested teleportation")
                    }));

            if (Decision == Decision.Accepted)
                args.Player.Teleport(player.X, player.Y);
        }

        public void TpAutoDenyCommand(CommandArgs args)
        {
            var index = args.Player.Index;
            AutoDeny[index] = !AutoDeny[index];

            for (int i = 0; i < 255; i++)
                if (TShock.Players[i] != null && TShock.Players[i].Active)
                RequestsManager.Block(TShock.Players[index], "tp", TShock.Players[i], AutoDeny[index]);
            args.Player.SendInfoMessage("TPAutoDeny {0}abled", AutoDeny[index] ? "en" : "dis");
        }

        public void OnGreet(GreetPlayerEventArgs args)
        {
            for (int i = 0; i < AutoDeny.Length; i++)
                if (AutoDeny[i])
                    RequestsManager.Block(TShock.Players[i], "tp", TShock.Players[args.Who], true);
        }
    }
}
