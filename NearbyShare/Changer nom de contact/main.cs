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
using NearbyShare.Partage_de_contact;
using UnityEngine;


namespace NearbyShare.Changer_nom_de_contact
{
    public class management : ModKit.ModKit
    {
        public management(IGameAPI api) : base(api) { }

        public async void ContactList(Player player)
        {
            // Affiche un menu de la liste des contacts
            ContactsList contactsList = await LifeDB.FetchContacts(player.character.Id);
            Panel menu = PanelHelper.Create("Liste des contacts", Life.UI.UIPanel.PanelType.TabPrice, player, () => ContactList(player));
            foreach (Contact contact in contactsList.contacts)
            {
                menu.AddTabLine(contact.name, contact.number, 101, ui => Selection(player, contact));
            }
            menu.PreviousButton();
            menu.CloseButton();
            menu.AddButton("Sélectionner", ui => menu.SelectTab());
            menu.Display();
        }

        public void Selection(Player player, Contact contact)
        {
            contact partage = new contact(api);
            Panel menu = PanelHelper.Create("Informations", Life.UI.UIPanel.PanelType.Text, player, () => ContactList(player));

            menu.PreviousButton();
            menu.AddButton("Renommer", ui => AskName(player, contact));
            menu.AddButton("<color=#FF0000>Supprimer", ui => DeleteConfirmation(player, contact));
            menu.AddButton("<color=#48c9b0>Partager", ui => partage.PreDemande(player, contact));

            menu.TextLines.Add("Informations de contact");
            menu.TextLines.Add($"Nom : {contact.name}");
            menu.TextLines.Add($"Numéro : {contact.number}");

            menu.Display();
        }

        // ----------------------------------------------------------------------------- Suppression

        public void DeleteConfirmation(Player player, Contact contact)
        {
            Panel menu = PanelHelper.Create("Confirmation", Life.UI.UIPanel.PanelType.Text, player, () => DeleteConfirmation(player, contact));
            menu.PreviousButton();
            menu.AddButton("Valider", ui => DeleteContact(player, contact));

            menu.TextLines.Add($"Êtes-vous sûr de vouloir supprimer le contact de {contact.name} ?");
            menu.TextLines.Add("<color=#FF0000>Attention : Cette action est irréversible !");
            menu.Display() ;
        }

        public async void DeleteContact(Player player, Contact contact)
        {
            await LifeDB.DeleteContact(contact.id);
            Panel menu = PanelHelper.Create("Contact supprimé", Life.UI.UIPanel.PanelType.Text, player, () => DeleteContact(player, contact));
            menu.CloseButton();
            menu.TextLines.Add("Le contact a été supprimé avec succès.");
            menu.Display();
        }

        // ----------------------------------------------------------------------------- Édition

        public void AskName(Player player, Contact contact)
        {
            Panel menu = PanelHelper.Create("NearbyShare", Life.UI.UIPanel.PanelType.Input, player, () => AskName(player, contact));
            menu.SetInputPlaceholder($"Nouveau nom de {contact.name}");
            menu.CloseButton();
            menu.AddButton("Valider", ui => EditName(player, contact, menu.inputText));
            menu.Display();
        }

        public async void EditName(Player player, Contact contact, string name)
        {
            string number = contact.number;
            await LifeDB.DeleteContact(contact.id);
            await LifeDB.CreateContact(player.character.Id, number, name);
            player.Notify("NearbyShare", "Contact renommé avec succès", NotificationManager.Type.Success);
        }
    }
}
