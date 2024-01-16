using System;
using Life;
using Life.DB;
using Life.Network;
using Life.UI;
using UnityEngine;

namespace NearbyShare
{
    public class NearbyShare:Plugin
    {
        public NearbyShare(IGameAPI api) : base(api) // Constructeur
        {

        }

        public override void OnPluginInit() // Démarrage du serveur
        {
            base.OnPluginInit();
            Debug.Log("[PLUGIN] Successfully started NearbyShare");
        }

        public DateTime cooldown = DateTime.Now;

        public bool TestCooldown(DateTime tempsActuel, Player player)
        {
            if (tempsActuel.Ticks-cooldown.Ticks > TimeSpan.FromSeconds(10).Ticks)
            {
                cooldown = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Refus(Player player, Player closePlayer, UIPanel panel)
        {
            closePlayer.ClosePanel(panel);
            player.Notify("NearbyShare", "La personne a refusé votre demande", NotificationManager.Type.Error);
            closePlayer.Notify("NearbyShare", "La demande a été refusée", NotificationManager.Type.Warning);
        }

        public void Accepter(Player player, Player closePlayer, UIPanel demande)
        {
            player.Notify("NearbyShare", "La demande a été acceptée !", NotificationManager.Type.Success);
            closePlayer.Notify("NearbyShare", "La demande a été acceptée !", NotificationManager.Type.Success);
            closePlayer.ClosePanel(demande);
            string phoneNumberPlayer = player.character.PhoneNumber;
            string phoneNumberClosePlayer = closePlayer.character.PhoneNumber;
            LifeDB.CreateContact(player.character.Id, phoneNumberClosePlayer, closePlayer.GetFullName());
            LifeDB.CreateContact(closePlayer.character.Id, phoneNumberPlayer, player.GetFullName());
        }

        public void Demande(Player player, Player closePlayer, UIPanel validation)
        {
            if (TestCooldown(DateTime.Now, player))
            {
                player.Notify("NearbyShare", "La demande a bien été envoyée !", NotificationManager.Type.Success);
                player.ClosePanel(validation);
                UIPanel demande = new UIPanel("NearbyShare", UIPanel.PanelType.Text);
                demande.SetText($"{player.GetFullName()} demande de partager votre numéro.");
                demande.AddButton("Refuser", ui => Refus(player, closePlayer, demande));
                demande.AddButton("Accepter", ui => Accepter(player, closePlayer, demande));
                closePlayer.ShowPanelUI(demande);
            }
            else
            {
                player.Notify("Trop rapide !", "Réessaie dans quelques secondes", NotificationManager.Type.Info);
                player.ClosePanel(validation);
            }
            
        }
        

        public override void OnPlayerInput(Player player, KeyCode keyCode, bool onUI)
        {
            if(!onUI && keyCode == KeyCode.Alpha7)
            {
                if (player.GetClosestPlayer() != null)
                {
                    Player closePlayer = player.GetClosestPlayer();
                    UIPanel validation = new UIPanel("NearbyShare", UIPanel.PanelType.Text);
                    validation.SetText($"Souhaitez-vous partager votre numéro à {closePlayer.GetFullName()}");
                    validation.AddButton("Refuser", ui => player.ClosePanel(validation));
                    validation.AddButton("Accepter", ui => Demande(player, closePlayer, validation));
                    player.ShowPanelUI(validation);
                }
                else
                {
                    player.Notify("Erreur", "Il n'y a personne autour de vous", NotificationManager.Type.Error);
                }
            }
        }
    }
}
