using System.Threading.Tasks;
using ModKit.Helper;
using Life;
using Life.Network;
using Life.DB;

namespace NearbyShare.Partage_de_proximite
{
    public class proxi : ModKit.ModKit
    {

        public proxi(IGameAPI api) : base (api) { }

        bool askerHasContact = false;
        bool receiverHasContact = false;
        bool isLocked = false;

        // Configuation de la protection Anti-Spam (= cooldown)

        public async void PreDemande(Player player) // Vérification avant envoi de la demande
        {
            Player closePlayer = player.GetClosestPlayer();
            Panel menu = PanelHelper.Create("NearbyShare", Life.UI.UIPanel.PanelType.Text, player, () => PreDemande(player));
            menu.CloseButton();
            if (closePlayer == null)
            {
                player.Notify("NearbyShare", "Il n'y a personne autour de vous", NotificationManager.Type.Error);
                isLocked = true;
            }
            else
            {
                (bool, string) testContact = await HaveContact(player, closePlayer);
                (bool, string) testContactClose = await HaveContact(closePlayer, player);
                if (testContact.Item1 && testContactClose.Item1)
                {
                    player.Notify("NearbyShare", $"{closePlayer.FullName} vous a déjà dans ses contacts et vous l'avez également dans les votres.", NotificationManager.Type.Error);
                    isLocked = true;
                }
                else if (testContact.Item1)
                {
                    menu.TextLines.Add($"{closePlayer.FullName} est déjà dans vos contacts.");
                    menu.TextLines.Add("Souhaitez-vous partager votre numéro ?");
                    askerHasContact = true;
                }
                else if (testContactClose.Item1)
                {
                    menu.TextLines.Add($"{closePlayer.FullName} a déjà votre numéro dans ses contacts.");
                    menu.TextLines.Add("Souhaitez-vous demander le sien ?");
                    receiverHasContact = true;
                }
                else
                {
                    menu.TextLines.Add($"Voulez-vous donner votre numéro et recevoir celui de {closePlayer.FullName} ?");
                }
                menu.AddButton("Valider", ui =>
                {
                    player.ClosePanel(menu);
                    Demande(player, closePlayer);
                });
            }
            if (!isLocked)
            {
                menu.Display();
            }
        }

        public void Demande(Player player, Player closePlayer)
        {
            Panel menu = PanelHelper.Create("Nouvelle demande", Life.UI.UIPanel.PanelType.Text, closePlayer, () => Demande(player, closePlayer));
            menu.CloseButton();

            if (receiverHasContact)
            {
                menu.TextLines.Add(player.FullName + " souhaite récupérer votre numéro. Acceptez-vous ?");
            }
            else if (askerHasContact)
            {
                menu.TextLines.Add(player.FullName + " souhaite partager son numéro mais il vous a déjà dans vos contacts.");
                menu.TextLines.Add("Voulez-vous recevoir ce numéro ?");
            }
            else
            {
                menu.TextLines.Add(player.FullName + " souhaite vous partager son numéro et prendre le votre. Acceptez-vous ?");
            }

            menu.AddButton("Valider", ui =>
            {
                closePlayer.ClosePanel(menu);
                Partage(player, closePlayer);
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

        public void Partage(Player player, Player closePlayer) // Fonction principale
        {
            if (!askerHasContact)
            {
                AjoutContact(player, closePlayer.character.PhoneNumber, closePlayer.FullName);
                player.Notify("NearbyShare", "Personne ajoutée à vos contacts", NotificationManager.Type.Success);
                closePlayer.Notify("NearbyShare", "Tranfert du numéro effectué");
            }
            if (!receiverHasContact)
            {
                AjoutContact(closePlayer, player.character.PhoneNumber, player.FullName);
                closePlayer.Notify("NearbyShare", "Personne ajoutée à vos contacts", NotificationManager.Type.Success);
                player.Notify("NearbyShare", "Tranfert du numéro effectué");
            }
        }
    }
}
