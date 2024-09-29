using ModKit.Helper;
using ModKit.Internal;
using ModKit.Interfaces;
using Life;
using Life.DB;
using _menu = AAMenu.Menu;
using Life.Network;
using ModKit.Utils;
using NearbyShare.Partage_de_proximite;
using NearbyShare.Changer_nom_de_contact;

namespace NearbyShare
{
    public class NearbyShare : ModKit.ModKit
    {
        
        public NearbyShare(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "2.0.1", "IceCubeFr");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            Menu();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "launched");
        }

        public void Menu()
        {
            _menu.AddInteractionTabLine(PluginInformations, "NearbyShare", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                MainMenu(player);
            });
        }        

        public async void MainMenu(Player player)
        {
            proxi proxi = new proxi(api);
            management management = new management(api);
            // Affiche un menu de la liste des contacts
            ContactsList contactsList = await LifeDB.FetchContacts(player.character.Id);
            Panel menu = PanelHelper.Create("Liste des contacts", Life.UI.UIPanel.PanelType.TabPrice, player, () => MainMenu(player));
            menu.AddTabLine("Partager mon numéro", ui => proxi.PreDemande(player));
            foreach (Contact contact in contactsList.contacts)
            {
                menu.AddTabLine(contact.name, contact.number, ItemUtils.GetIconIdByItemId(1190), ui => management.Selection(player, contact));
            }
            menu.CloseButton();
            menu.AddButton("Sélectionner", ui => menu.SelectTab());
            menu.Display();
        }


    }
}