using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Life;
using Newtonsoft.Json;
using Life.DB;
using Life.Network;
using Life.UI;
using UnityEngine;
using MyMenu.Entities;
using System.Diagnostics.Eventing.Reader;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NearbyShare
{
    public class NearbyShare : Plugin
    {
        public static string filename = "Config.json";

        public static string directoryPath;

        public KeyCode touche = KeyCode.Alpha7;

        public bool usingKey = true;

        public bool closePlayerHasContact = false;

        public KeyCode Switch(string touche)
        {
            switch(touche) 
            {
                case "&":
                case "1":
                    return KeyCode.Alpha1; 
                case "2":
                case "é":
                    return KeyCode.Alpha2;
                case "3":
                    return KeyCode.Alpha3;
                case "4":
                case "'":
                    return KeyCode.Alpha4;
                case "5":
                case "(":
                    return KeyCode.Alpha5;
                case "-":
                case "6":
                    return KeyCode.Alpha6;
                case "è":
                case "7":
                    return KeyCode.Alpha7;
                case "_":
                case "8":
                    return KeyCode.Alpha8;
                case "ç":
                case "9":
                    return KeyCode.Alpha9;
                case "à":
                case "0":
                    return KeyCode.Alpha0;
                case "^":
                    return KeyCode.RightBracket;
                case ")":
                    return KeyCode.LeftBracket;
                case "=":
                    return KeyCode.Equals;
                case "$":
                    return KeyCode.Semicolon;
                case "ù":
                case "!":
                    return KeyCode.BackQuote;
                case "*":
                    return KeyCode.Backslash;
                case ",":
                    return KeyCode.Comma;
                case ";":
                    return KeyCode.Period;
                case ":":
                    return KeyCode.Slash;
                default:
                    return KeyCode.Alpha7;
            }
        }

        public void SaveJson()
        {
            List<object> info = new List<object>
                        {
                            touche.ToString(),
                            usingKey
                        };
            string jsonFile = Directory.GetFiles(directoryPath, filename).FirstOrDefault();
            string updatedJson = JsonConvert.SerializeObject(info, Formatting.Indented);
            File.WriteAllText(jsonFile, updatedJson);
        }

        public NearbyShare(IGameAPI api) : base(api) // Constructeur
        {

        }
        

        public override void OnPluginInit() // Démarrage du serveur
        {
            base.OnPluginInit();

            // Récupération ou création des données Json
            InitDirectory();
            try 
            {
                string jsonFile = Directory.GetFiles(directoryPath, filename).FirstOrDefault();

                if (jsonFile != null)
                {
                    string json = File.ReadAllText(jsonFile);
                    List<object> setup = JsonConvert.DeserializeObject<List<object>>(json);
                    string nouvelleTouche = Convert.ToString(setup[0]);
                    touche = (KeyCode)Enum.Parse(typeof(KeyCode), nouvelleTouche, true);
                    usingKey = Convert.ToBoolean(setup[1]);
                    SaveJson();
                }
                else
                {
                    List<object> info = new List<object>
                    {
                        touche.ToString(),
                        usingKey
                    };
                    string filePath = Path.Combine(directoryPath, filename);
                    string json = JsonConvert.SerializeObject(info, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
            } 
            catch (Exception ex) 
            {
                Debug.Log("[PLUGIN] Failed to load Json file from NearbyShare : " + ex.Message);
            }

            // Commande de modification de la touche de base

            new SChatCommand("/nstouche", "Modifie la touche de votre NearbyShare", "/nstouche", (player, arg) =>
            {
                if (player.IsAdmin)
                {
                    if (arg.Length > 0)
                    {
                        if(arg[0].Length == 1)
                        {
                            Array valeurs = Enum.GetValues(typeof(KeyCode));
                            if (Enum.TryParse(arg[0], true, out KeyCode result))
                            {
                                touche = (KeyCode)Enum.Parse(typeof(KeyCode), arg[0], true);
                                player.Notify("NearbyShare", $"Touche définie sur {touche} !", NotificationManager.Type.Success);
                                
                            }
                            else
                            {
                                touche = Switch(arg[0]);
                                player.Notify("NearbyShare", $"Touche définie sur {touche}.", NotificationManager.Type.Success);
                            }
                            usingKey = true;
                            SaveJson();
                        }
                        else
                        {
                            player.Notify("NearbyShare", "Votre argument est incorrect", NotificationManager.Type.Error);
                        }
                    }
                    else
                    {
                        usingKey = false;
                        SaveJson();
                        player.Notify("NearbyShare", "La touche de base a été désactivée. Utilisation uniquement via MyMenu.", NotificationManager.Type.Info);
                    }
                }
            }).Register();

            // Initialisation MyMenu

            Section section = new Section(Section.GetSourceName(), Section.GetSourceName(), "v1.2", "IceCubeFr");
            Action<UIPanel> action = ui => Primaire(section.GetPlayer(ui));
            section.SetBizIdAllowed();
            section.SetBizTypeAllowed();
            section.OnlyAdmin = false;
            section.MinAdminLevel = 0;
            section.Line = new UITabLine(section.Title, action);
            section.Insert(false);

            // Fin de l'initialisation

            Debug.Log("[PLUGIN] Successfully started NearbyShare");
        }

        // Cooldown utilisation de la fonctionnalité

        public DateTime cooldown = DateTime.Now;

        public ListDictionary listeCooldown = new ListDictionary();

        public bool TestCooldown(DateTime tempsActuel, Player player)
        {
            if (listeCooldown.Contains(player.netId))
            {
                if (tempsActuel.Ticks - (long)listeCooldown[player.netId] > TimeSpan.FromSeconds(10).Ticks)
                {
                    listeCooldown[player.netId] = tempsActuel.Ticks;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                listeCooldown.Add(player.netId, tempsActuel.Ticks);
                return true;
            }
        }

        // Configuration des différents menus et de l'antispam

        public void Refus(Player player, Player closePlayer, UIPanel panel)
        {
            closePlayer.ClosePanel(panel);
            player.Notify("NearbyShare", "La personne a refusé votre demande", NotificationManager.Type.Error);
            closePlayer.Notify("NearbyShare", "La demande a été refusée", NotificationManager.Type.Warning);
        }

        public async void Accepter(Player player, Player closePlayer, UIPanel demande)
        {
            player.Notify("NearbyShare", "La demande a été acceptée !", NotificationManager.Type.Success);
            closePlayer.Notify("NearbyShare", "La demande a été acceptée !", NotificationManager.Type.Success);
            closePlayer.ClosePanel(demande);
            string phoneNumberPlayer = player.character.PhoneNumber;
            string phoneNumberClosePlayer = closePlayer.character.PhoneNumber;
            await LifeDB.CreateContact(player.character.Id, phoneNumberClosePlayer, closePlayer.GetFullName());

            // Vérification de la présence de player dans les contacts de closeplayer

            ContactsList contactsList = await LifeDB.FetchContacts(closePlayer.character.Id);
            if (contactsList.contacts == null)
            {
                closePlayerHasContact = false;
            }
            else
            {
                foreach (Contact contactActuel in contactsList.contacts)
                {
                    if (contactActuel.number == player.character.PhoneNumber)
                    {
                        closePlayerHasContact = true;
                    }
                }
            }
            if (!closePlayerHasContact)
            {
                await LifeDB.CreateContact(closePlayer.character.Id, phoneNumberPlayer, player.GetFullName());
            }
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

        // Fonction principale

        public async void Primaire(Player player)
        {
            if (player.GetClosestPlayer() != null)
            {
                bool possible = true;
                Player closePlayer = player.GetClosestPlayer();
                ContactsList contactsList = await LifeDB.FetchContacts(player.character.Id);
                
                if (contactsList.contacts == null)
                {
                    possible = true;
                }
                else
                {
                    foreach (Contact contactActuel in contactsList.contacts)
                    {
                        if (contactActuel.number == closePlayer.character.PhoneNumber)
                        {
                            player.Notify("NearbyShare", $"Vous avez déjà cette personne en contact au nom de {contactActuel.name}", NotificationManager.Type.Info);
                            possible = false;
                        }
                    }
                }
                if (possible)
                {
                    UIPanel validation = new UIPanel("NearbyShare", UIPanel.PanelType.Text);
                    validation.SetText($"Souhaitez-vous partager votre numéro à {closePlayer.GetFullName()}");
                    validation.AddButton("Refuser", ui => player.ClosePanel(validation));
                    validation.AddButton("Accepter", ui => Demande(player, closePlayer, validation));
                    player.ShowPanelUI(validation);
                }
            }
            else
            {
                player.Notify("Erreur", "Il n'y a personne autour de vous", NotificationManager.Type.Error);
            }
        }
        
        // Fonction si la touche est toujours activée

        public override void OnPlayerInput(Player player, KeyCode keyCode, bool onUI)
        {
            if(!onUI && keyCode == touche && usingKey)
            {
                Primaire(player);
            }
        }

        public void InitDirectory()
        {
            directoryPath = pluginsPath + "/NearbyShare";
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
    }
}
