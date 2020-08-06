using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipsClient
{
    public class i18n
    {
        public static CultureInfo Language=CultureInfo.CurrentCulture;
        public static CultureInfo Defaultlanguage = CultureInfo.GetCultureInfo("en-US");
        public static CultureInfo[] LanguageList = { CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("en-US") };
        private static  Dictionary<CultureInfo, Dictionary<string, string>> LanguageTextList = new Dictionary<CultureInfo, Dictionary<string, string>>();
        private static int iTextDefinitions = 1;
        private static Dictionary<CultureInfo,Dictionary<string, string>> TextTable = new Dictionary<CultureInfo,Dictionary<string, string>>()
        {
            {
               CultureInfo.GetCultureInfo("de-DE"),new Dictionary<string, string>()
               {
                   {"shipName1","U - Boot" },
                   {"shipName2","Zerstörer 1" },
                   {"shipName3","Zerstörer 2" },
                   {"shipName4","Kreuzer" },
                   {"shipName5","Schlachtschiff" },
                   {"shipPlaced","Plaziert!" },
                   {"shipSelected","{0} ausgewählt" + Environment.NewLine + "Schiffslänge: {1} Kästchen" + Environment.NewLine },
                   {"buttonFire","Schuss !" },
                   {"buttonReset","Zurücksetzen" },
                   {"buttonSubmit","Spiel übermitteln" },
                   {"toolMenuSurrender","Aufgeben" },
                   {"EnemyWater","Feindliches Gewässer" },
                   {"FriendlyWater","Eigenes Gewässer" },
                   {"gameLog","Spielverlauf"},
                   {"formBattleShip","Schiffe versenken"},
                   {"welcomeMessage","Willkommen zu Schiffe versenken! Bitte setze Deine Schiffe in Dein Gewässer. Wähle das Schiff durch drücken des jeweiligen Buttons und klicke anschließend im Gewässer die Anfangs- und Endposition des Schiffes. " + Environment.NewLine},
                   {"placementDescription","Schiffsposition festlegen:" + Environment.NewLine + "1) Schifftyp auswählen" + Environment.NewLine + "2) Klicke im eignen Gewässer die Anfangsposition... " + Environment.NewLine + "3) und anschließend die Endposition des Schiffes" + Environment.NewLine + "4) Sind alle Schiffe platziert drücke \"Spiel übermitteln\"" + Environment.NewLine },
                   {"firstShotMe","Mach den ersten Schuss, wähle eine Postition im feindlichen Gewässer!" + Environment.NewLine },
                   {"firstShotEnemy","Der Gegner hat den ersten Schuss. Warte auf den Einschlag..." + Environment.NewLine },
                   {"shotHit","Du hast getroffen !!!" + Environment.NewLine},
                   {"oneShipHit","Eines Deiner Schiffe wurde getroffen!!" + Environment.NewLine},
                   {"shotMissed","Du hast nicht getroffen :-(" + Environment.NewLine},
                   {"waitEnemyShot","Warte auf den Schuss Deines Gegners." + Environment.NewLine},
                   {"turnToShot","Du bist dran mit schiessen" + Environment.NewLine},
                   {"opponentMissedFleet","Dein Gegner hat mit dem Schuss die Flotte verfehlt!" + Environment.NewLine},
                   {"wonCongratulations","Gratulation {0}, Du hast gewonnen!" + Environment.NewLine + "Wenn Du das Fenster schliesst wird das Spiel beendet."},
                   {"lossGame","{0} Du hast leider verloren." + Environment.NewLine + "Wenn Du das Fenster schliesst wird das Spiel beendet."},
                   {"invalidPlacement","Ungültige Plazierung eines Schiffes: {0} Versuchs nochmal."},
                   {"invalidPlacementTitle","Ungültige Plazierung"},
                   {"chatFormTtile","Chat Raum" },
                   {"signOut","Abmelden" },
                   {"quit","Beenden" },
                   {"connectionLost","Verbindung zu Server verloren... melde mich ab" },
                   {"connectionLostTitle", "Verbindung  verloren!" },
                   {"challengeRequest", "Schlachtanfrage an {0} gesendet." + Environment.NewLine + "Warte auf Antwort..." },
                   {"challengeRequestAborted", "Schlachtanfrage abgebrochen." },
                   {"ChallengeAUserRequest","{0}" + Environment.NewLine + "Gewonnene Schlachten: {1}" +  Environment.NewLine + "Verlorene Schlachten: {2}"  + Environment.NewLine + "Herausforderung annehmen ?"},
                   {"ChallengeAUsertitle", "Benutzer herausfordern" },
                   {"userNotFound", "Benutzer nicht gefunden! Bitte Liste aktualisieren..." },
                   {"userNotFoundTitle", "Fehler - Benutzer nicht gefunden!" },
                   {"ChallengeRejected", "Deine Spieleanfrage wurde abgelehnt" +Environment.NewLine},
                   {"yourWins", "Gewonnen: {0}" },
                   {"yourLosses", "Verloren: {0}" },
                   {"ChallengeRequest", "{0} hat Dich zur Schlacht herausgefordert!" + Environment.NewLine 
                        + "Gewonnene Schlachten: : {1}" + Environment.NewLine
                        + "Verlorene Schlachten: : {2}" +Environment.NewLine
                        + "Willst Du annehmen ? " },
                   {"ChallengeRequestTitle","Du bist herausgefordert worden !" },
                   {"userHasDisconected", "{0} hat die Verbindung getrennt." + Environment.NewLine },
                   {"userHasConnected", "{0} hat sich verbunden." + Environment.NewLine },
                   {"challenge", "Herausfordern!"},
                   {"sendMessage", "Senden!"},
                   {"connectedUsers", "Verbundene Benutzer"}
               }
            },
            {
               CultureInfo.GetCultureInfo("en-US"),new Dictionary<string, string>()
               {
                   {"shipName1","Destroyer" },
                   {"shipName2","Cruiser" },
                   {"shipName3","Submarine" },
                   {"shipName4","Battleship" },
                   {"shipName5","Carrier" },
                   {"shipPlaced","Placed!" },
                   {"shipSelected","{0} selected" + Environment.NewLine + "Ship length: {1} grid squares" + Environment.NewLine},
                   {"buttonFire","Fire !" },
                   {"buttonReset","Reset" },
                   {"buttonSubmit","Submit game" },
                   {"toolMenuSurrender","Surrender"},
                   {"EnemyWater","Enemy Waters"},
                   {"FriendlyWater","Friendly Waters"},
                   {"gameLog","Spielverlauf"},
                   {"formBattleShip","Battleship Game"},
                   {"welcomeMessage","Welcome To Battleships! Please begin by placing your ships using the controls below the game board." + Environment.NewLine},
                   {"placementDescription","Ship Placement:" + Environment.NewLine + "1)Select a ship type" + Environment.NewLine + "2) Select a location for the front of your ship" + Environment.NewLine + "3) Select a location for the rear of your ship" + Environment.NewLine + "4) Once all ships have been placed, press the Submit button" + Environment.NewLine },
                   {"firstShotMe","It is your turn to shoot first, please select a position within enemy waters" + Environment.NewLine },
                   {"firstShotEnemy","It is your opponent's turn to shoot first. Waiting for response..." + Environment.NewLine },
                   {"shotHit","Your shot hit!" + Environment.NewLine},
                   {"oneShipHit","One of your ships has been hit!" + Environment.NewLine},
                   {"shotMissed","Your shot missed!" + Environment.NewLine},
                   {"waitEnemyShot","It is your opponent's turn to shoot." + Environment.NewLine},
                   {"turnToShot","It is your turn to shoot." + Environment.NewLine},
                   {"opponentMissedFleet","Your opponent missed your fleet!" + Environment.NewLine},
                   {"wonCongratulations","Congratulations {0} you have won the game!" + Environment.NewLine + "Closing this dialog will close the game window."},
                   {"lossGame","Sorry {0} you have lost the game." + Environment.NewLine + "Closing this dialog will close the game window."},
                   {"invalidPlacement","Invalid ship placement: {0} Please try again."},
                   {"invalidPlacementTitle","Invalid Placement"},
                   {"chatFormTtile","Chat Raum" },
                   {"signOut","Sign Out" },
                   {"quit","Quit" },
                   {"connectionLost","Connection to server lost. Signing out..." },
                   {"connectionLostTitle", "Connection Lost!" },
                   {"challengeRequest", "Challenge request sent to {0}." + Environment.NewLine + "Waiting for a response..." },
                   {"challengeRequestAborted", "Challenge aborted."  },
                   {"ChallengeAUserRequest","{0}" + Environment.NewLine + "Wins: {1}" + Environment.NewLine + "Losses: {2}" + Environment.NewLine + "Challenge this user?"},
                   {"ChallengeAUsertitle", "Challenge a User" },
                   {"userNotFound", "User could not be found! Refreshing user list..." },
                   {"userNotFoundTitle", "Error - User Not Found!" },
                   {"ChallengeRejected", "Your game challenge was rejected."+Environment.NewLine },
                   {"yourWins", "Your Wins: {0}" },
                   {"yourLosses", "Your Losses: {0}" },
                   {"ChallengeRequest", "{0} has challenged you!" + Environment.NewLine 
                        + "Wins: {1}" + Environment.NewLine
                        + "Losses: {2}" +  Environment.NewLine
                        + "Do You accept?" },
                    {"ChallengeRequestTitle","You have been challenged!" },
                    {"userHasDisconected", "{0} has disconnected." + Environment.NewLine },
                    {"userHasConnected", "{0} has connected." + Environment.NewLine},
                    {"challenge", "Challenge!"},
                    {"sendMessage", "Send"},
                    {"connectedUsers", "Connected Users"}
               }
            }
        };
        public static string GetText(string textname)
        {
            if(TextTable.ContainsKey(Language))
            {
                if (TextTable[Language].ContainsKey(textname))
                {
                    return (TextTable[Language][textname]);
                }
                else
                {
                    throw new Exception("Requested text not found in TextTable");
                }
            }
            else
            {
                if(TextTable[Defaultlanguage].ContainsKey(textname))
                {
                    return (TextTable[Defaultlanguage][textname]);
                }
                else
                {
                    throw new Exception("Requested text not found in TextTable");
                }
            }
        }
        public static string GetText(string textname,string TextVariable)
        {
            if (TextTable.ContainsKey(Language))
            {
                if (TextTable[Language].ContainsKey(textname))
                {
                    return (string.Format(TextTable[Language][textname], TextVariable));
                }
                else
                {
                    throw new Exception("Requested text not found in TextTable");
                }
            }
            else
            {
                if (TextTable[Defaultlanguage].ContainsKey(textname))
                {
                    return (string.Format(TextTable[Defaultlanguage][textname], TextVariable));
                }
                else
                {
                    throw new Exception("Requested text not found in TextTable");
                }
            }
        }
        public static string GetText(string textname, string TextVariable1, string TextVariable2)
        {
            if (TextTable.ContainsKey(Language))
            {
                if (TextTable[Language].ContainsKey(textname))
                {
                    return (string.Format(TextTable[Language][textname], TextVariable1, TextVariable2));
                }
                else
                {
                    throw new Exception("Requested text not found in TextTable");
                }
            }
            else
            {
                if (TextTable[Defaultlanguage].ContainsKey(textname))
                {
                    return (string.Format(TextTable[Defaultlanguage][textname], TextVariable1, TextVariable2));
                }
                else
                {
                    throw new Exception("Requested text not found in TextTable");
                }
            }
        }
        public static string GetText(string textname, string TextVariable1, string TextVariable2, string TextVariable3)
        {
            if (TextTable.ContainsKey(Language))
            {
                if (TextTable[Language].ContainsKey(textname))
                {
                    return (string.Format(TextTable[Language][textname], TextVariable1, TextVariable2, TextVariable3));
                }
                else
                {
                    throw new Exception("Requested text not found in TextTable");
                }
            }
            else
            {
                if (TextTable[Defaultlanguage].ContainsKey(textname))
                {
                    return (string.Format(TextTable[Defaultlanguage][textname], TextVariable1, TextVariable2, TextVariable3));
                }
                else
                {
                    throw new Exception("Requested text not found in TextTable");
                }
            }
        }
    }
}
