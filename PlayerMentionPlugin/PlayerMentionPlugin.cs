using System;
using System.Text.RegularExpressions;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PlayerMention
{
    [ApiVersion(2, 1)]
    public class PlayerMentionPlugin : TerrariaPlugin
    {
        public override string Name => "PlayerMention";
        public override string Author => "AI";
        public override string Description => "Автоматически заменяет @часть_ника на @ПолныйНик игрока (цветной) в чате.";
        public override Version Version => new Version(1, 0, 17);

        public PlayerMentionPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.ServerChat.Register(this, OnServerChat);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerChat.Deregister(this, OnServerChat);
            }
            base.Dispose(disposing);
        }

        private void OnServerChat(ServerChatEventArgs e)
        {
            if (e.Handled || e.Text.StartsWith("/") || e.Text.StartsWith(".")) 
                return;

            TSPlayer player = TShock.Players[e.Who];
            if (player == null || !player.Active)
                return;

            if (!e.Text.Contains("@"))
                return;

            bool insideMention = false;

            string modifiedText = Regex.Replace(e.Text, @"@([a-zA-Z0-9_а-яА-ЯёЁ]+)", match =>
            {
                string partialName = match.Groups[1].Value;
                var foundPlayers = TSPlayer.FindByNameOrID(partialName);
                
                if (foundPlayers.Count > 0)
                {
                    var targetPlayer = foundPlayers[0];

                    if (targetPlayer.RealPlayer && targetPlayer.Index != player.Index)
                    {
                        targetPlayer.SendData((PacketTypes)29, "", 29, targetPlayer.X, targetPlayer.Y, 105f);
                    }

                    insideMention = true;
                    return $"[c/f58e18:@{targetPlayer.Name}]";
                }

                return match.Value;
            });

            if (insideMention)
            {
                // Отменяем стандартный пакет чата сервера
                e.Handled = true;

                var group = player.Group;
                string prefix = group?.Prefix ?? "";
                string suffix = group?.Suffix ?? "";

                // Вручную склеиваем строку в точном соответствии со стандартным видом: ПрефиксИмяСуффикс: Текст
                // Никаких плейсхолдеров {0}, {1}, {2}, {3} — чистый, готовый текст.
                string finalMessage = $"{prefix}{player.Name}{suffix}: {modifiedText}";

                // Цвета текста берем из группы игрока
                byte r = (byte)(group?.R ?? 255);
                byte g = (byte)(group?.G ?? 255);
                byte b = (byte)(group?.B ?? 255);

                // Отправляем готовую строку в чат и дублируем в консоль сервера
                TShock.Utils.Broadcast(finalMessage, r, g, b);
            }
        }
    }
}