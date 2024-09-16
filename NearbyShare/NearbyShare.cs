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
using NearbyShare.Partage_de_proximite;
using NearbyShare.Changer_nom_de_contact;

namespace NearbyShare
{
    public class NearbyShare : ModKit.ModKit
    {
        
        public NearbyShare(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "2.0.0", "IceCubeFr");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            Menu();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "completely started");
        }

        public void Menu()
        {
            _menu.AddInteractionTabLine(PluginInformations, "Téléphone", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                MainMenu(player);
            });
        }        

        public void MainMenu(Player player)
        {
            proxi proxi = new proxi(api);
            management management = new management(api);
            Panel mainMenu = PanelHelper.Create("Téléphone", Life.UI.UIPanel.PanelType.Tab, player, () => MainMenu(player));
            mainMenu.CloseButton();
            mainMenu.AddButton("Valider", ui =>
            {
                mainMenu.SelectTab();
                player.ClosePanel(mainMenu);
            });
            mainMenu.AddTabLine("Partager mon numéro", ui => proxi.PreDemande(player));
            mainMenu.AddTabLine("Gestion de mes contacts", ui => management.ContactList(player));
            mainMenu.Display();
        }


    }
}