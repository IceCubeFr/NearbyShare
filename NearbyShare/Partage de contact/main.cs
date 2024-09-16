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
using UnityEngine;

namespace NearbyShare.Partage_de_contact
{
    public class contact : ModKit.ModKit
    {
        public contact(IGameAPI api) : base(api) { }

        public async Task<(bool, string)> HaveContact(Player player, Contact contactTest)
        {
            //
            //  Vérifie si le player a le player2 dans sa liste de contacts
            //
            bool haveContact = false;
            string nomContact = "erreur";
            ContactsList contactsList = await LifeDB.FetchContacts(player.character.Id);
            foreach (Contact contact in contactsList.contacts)
            {
                if (contact.number == contactTest.number)
                {
                    haveContact = true;
                    nomContact = contact.name;
                }
            }
            return (haveContact, nomContact);
        }

        public async void PreDemande(Player player, Contact contact) // Vérification avant envoi de la demande
        {
            Player closePlayer = player.GetClosestPlayer();
            if (closePlayer == null)
            {
                player.Notify("Erreur", "Il n'y a personne autour de vous", NotificationManager.Type.Error);
            }
            (bool, string) testContact = await HaveContact(closePlayer, contact);
            if (testContact.Item1)
            {
                player.Notify("NearbyShare", "Cette personne a déjà ce contact", NotificationManager.Type.Error);
            }
            else
            {
                Panel menu = PanelHelper.Create("NearbyShare", Life.UI.UIPanel.PanelType.Text, player, () => PreDemande(player, contact));
                menu.CloseButton();
                menu.AddButton("Valider", ui =>
                {
                    player.ClosePanel(menu);
                    Demande(player, contact);
                });
                menu.TextLines.Add($"Êtes-vous sur de vouloir partager le contact de {contact.name} à {closePlayer.GetFullName()} ?");
                menu.Display();

            }
        }

        public void Demande(Player player, Contact contact)
        {
            Player closePlayer = player.GetClosestPlayer();
            Panel menu = PanelHelper.Create("Nouvelle demande", Life.UI.UIPanel.PanelType.Text, closePlayer, () => Demande(player, contact));
            menu.CloseButton();
            menu.AddButton("Valider", ui =>
            {
                closePlayer.ClosePanel(menu);
                Partage(player, contact);
            });
            menu.SetTitle("Nouvelle Demande");
            menu.TextLines.Add($"{player.GetFullName()} souhaite vous partager un de ses contacts. Acceptez-vous ?");
            menu.Display();
        }

        public async void Partage(Player player, Contact contact)
        {
            Player closePlayer = player.GetClosestPlayer();
            await LifeDB.CreateContact(closePlayer.character.Id, contact.number, contact.name);
            closePlayer.Notify("NearbyShare", "Personne ajoutée à vos contacts", NotificationManager.Type.Success);
            player.Notify("NearbyShare", "Tranfert du numéro effectué");
        }
    }
}
