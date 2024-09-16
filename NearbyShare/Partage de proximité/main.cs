using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModKit.Helper;
using ModKit.Internal;
using ModKit.Interfaces;
using Life;
using _menu = AAMenu.Menu;
using Life.Network;
using System.Collections.Specialized;
using NearbyShare;
using NearbyShare.Changer_nom_de_contact;
using Life.DB;
using System.Threading;

namespace NearbyShare.Partage_de_proximite
{
    public class proxi : ModKit.ModKit
    {

        public proxi(IGameAPI api) : base (api) { }

        // Configuation de la protection Anti-Spam (= cooldown)

        public async void PreDemande(Player player) // Vérification avant envoi de la demande
        {
            Player closePlayer = player.GetClosestPlayer();
            int id = 0;
            Panel menu = PanelHelper.Create("NearbyShare", Life.UI.UIPanel.PanelType.Text, player, () => PreDemande(player));
            menu.CloseButton();
            if (closePlayer == null)
            {
                player.Notify("NearbyShare", "Il n'y a personne autour de vous", NotificationManager.Type.Error);
                id = 3;
            }
            else
            {
                (bool, string) testContact = await HaveContact(player, closePlayer);
                (bool, string) testContactClose = await HaveContact(closePlayer, player);
                if (testContact.Item1 && testContactClose.Item1)
                {
                    player.Notify("NearbyShare", $"{closePlayer.GetFullName()} vous a déjà dans ses contacts et vous l'avez également dans les votres.", NotificationManager.Type.Error);
                    id = 3;
                }
                else if (testContact.Item1)
                {
                    menu.TextLines.Add($"{closePlayer.GetFullName()} est déjà dans vos contacts.");
                    menu.TextLines.Add("Souhaitez-vous partager votre numéro ?");
                    id = 1;
                }
                else if (testContactClose.Item1)
                {
                    menu.TextLines.Add($"{closePlayer.GetFullName()} a déjà votre numéro dans ses contacts.");
                    menu.TextLines.Add("Souhaitez-vous demander le sien ?");
                    id = 2;
                }
                else
                {
                    menu.TextLines.Add($"Voulez-vous donner votre numéro et recevoir celui de {closePlayer.GetFullName()} ?");
                }
                menu.AddButton("Valider", ui =>
                {
                    player.ClosePanel(menu);
                    Demande(player, id);
                });
            }
            if (id < 3)
            {
                menu.Display();
            }
        }

        public void Demande(Player player, int id)
        {
            Player closePlayer = player.GetClosestPlayer();
            Panel menu = PanelHelper.Create("Nouvelle demande", Life.UI.UIPanel.PanelType.Text, closePlayer, () => Demande(player, id));
            menu.CloseButton();

            if (id == 1)
            {
                menu.TextLines.Add(player.GetFullName() + " souhaite récupérer votre numéro. Acceptez-vous ?");
            }
            else if (id == 2)
            {
                menu.TextLines.Add(player.GetFullName() + " souhaite partager son numéro mais vous l'avez déjà dans vos contacts.");
                menu.TextLines.Add("Voulez-vous recevoir ce numéro ?");
            }
            else
            {
                menu.TextLines.Add(player.GetFullName() + " souhaite vous partager son numéro et prendre le votre. Acceptez-vous ?");
            }

            menu.AddButton("Valider", ui =>
            {
                closePlayer.ClosePanel(menu);
                Partage(player, closePlayer, id);
            });
            menu.Display();
            
        }

        public async void AjoutContact(Player player, string numero, string nomContact)
        {
            await LifeDB.CreateContact(player.character.Id, numero, nomContact);
        }

        public async Task<(bool, string)> HaveContact(Player player, Player player2)
        {
            //
            //  Vérifie si le player a le player2 dans sa liste de contacts
            //
            bool haveContact = false;
            string nomContact = "erreur";
            ContactsList contactsList = await LifeDB.FetchContacts(player.character.Id);
            foreach (Contact contact in contactsList.contacts)
            {
                if (contact.number == player2.character.PhoneNumber)
                {
                    haveContact = true;
                    nomContact = contact.name;
                }
            }
            return (haveContact, nomContact);
        }

        public void Partage(Player player, Player closePlayer, int id) // Fonction principale
        {
            if (id == 2 || id == 0)
            {
                AjoutContact(player, closePlayer.character.PhoneNumber, closePlayer.GetFullName());
                player.Notify("NearbyShare", "Personne ajoutée à vos contacts", NotificationManager.Type.Success);
                closePlayer.Notify("NearbyShare", "Tranfert du numéro effectué");
            }
            if (id == 1 || id == 0)
            {
                AjoutContact(closePlayer, player.character.PhoneNumber, player.GetFullName());
                closePlayer.Notify("NearbyShare", "Personne ajoutée à vos contacts", NotificationManager.Type.Success);
                player.Notify("NearbyShare", "Tranfert du numéro effectué");
            }
        }
    }
}
