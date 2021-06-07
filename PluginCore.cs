using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;
using Newtonsoft.Json;
using TidyBags.Models;
using TidyBags.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using VirindiViewService;
using VirindiViewService.Controls;
using System.Linq;

namespace TidyBags
{
    //Attaches events from core
    [WireUpBaseEvents]

    //View (UI) handling
    [MVView("TidyBags.mainView.xml")]
    [MVWireUpControlEvents]

    [FriendlyName("TidyBags")]
    public class PluginCore : PluginBase
    {
        HudView questView;
        double actionPerSecond = 2.5;
        long lastRun = 0;
        bool sort = true;
        bool run = false;

        // Recurring Events
        Timer AllQuestRedrawTimer { get; set; }
        Timer AutoSaveTimer { get; set; }

        // Views
        
        // Data Repositories
        /// <summary>
        /// This is called when the plugin is started up. This happens only once.
        /// </summary>
        protected override void Startup()
        {
            try
            {
                // This initializes our static Globals class with references to the key objects your plugin will use, Host and Core.
                // The OOP way would be to pass Host and Core to your objects, but this is easier.
                Globals.Init("TidyBags", Host, Core);

                //Initialize the view.
                MVWireupHelper.WireupStart(this, Host);
                var views = HudView.GetAllViews();
                foreach (var view in views)
                {
                    if (view.Title == "Quest Helper")
                    {
                        questView = view;
                    }
                }

            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void LoadPlayerData()
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var preferencesFilePath = $"{appdata}\\VehnPlugins\\TidyBags\\QuestPreferences\\{Core.CharacterFilter.Server}\\{Core.CharacterFilter.AccountName}\\{Core.CharacterFilter.Name}.json";
            //_playerData = _playerDataRepository.Load(Core.CharacterFilter.Server, Core.CharacterFilter.AccountName, Core.CharacterFilter.Name);
            

            if (!File.Exists(preferencesFilePath))
            {
                CreateChildDirectories(preferencesFilePath);
                return;
            }

            var file = new StreamReader(preferencesFilePath);

            var preferencesJson = file.ReadToEnd();

            //_playerData = JsonConvert.DeserializeObject<PlayerData>(preferencesJson);
            file.Close();
        }

        private void CreateChildDirectories(string preferencesFilePath)
        {
            var lastSlash = preferencesFilePath.LastIndexOf('\\');
            var directory = preferencesFilePath.Substring(0, lastSlash);
            if (!Directory.Exists(directory))
            {
                CreateChildDirectories(directory);
                Directory.CreateDirectory(directory);
            }
        }



        /// <summary>
        /// This is called when the plugin is shut down. This happens only once.
        /// </summary>
        protected override void Shutdown()
        {
            try
            {
                //Destroy the view.
                MVWireupHelper.WireupEnd(this);
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void LoadState()
        {
            LoadPlayerData();
        }

        private void SavePlayerData()
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var preferencesFilePath = $"{appdata}\\VehnPlugins\\TidyBags\\QuestPreferences\\{Core.CharacterFilter.Server}\\{Core.CharacterFilter.AccountName}\\{Core.CharacterFilter.Name}.json";

            //var text = JsonConvert.SerializeObject(_playerData, Formatting.Indented);
            var f = new StreamWriter(preferencesFilePath);
            //f.Write(text);
            f.Close();
        }

        [BaseEvent("LoginComplete", "CharacterFilter")]
        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            try
            {
                Core.RenderFrame += DoWork;
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void DoWork(object sender, EventArgs e)
        {
            var actionSpeed = TimeSpan.FromSeconds(1.0 / actionPerSecond).Ticks;
            if (run && DateTime.UtcNow.Ticks > lastRun + actionSpeed)
            {
                lastRun = DateTime.UtcNow.Ticks;
                Step();
            }
        }

        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
                AllQuestRedrawTimer.Stop();
                Core.RenderFrame -= DoWork;
                // Unsubscribe to events here, but know that this event is not gauranteed to happen. I've never seen it not fire though.
                // This is not the proper place to free up resources, but... its the easy way. It's not proper because of above statement.
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("EditQuestCancel", "Click")]
        void EditQuestCancel_Click(object sender, MVControlEventArgs e)
        {
        }


        [MVControlEvent("EditQuestSave", "Click")]
        void EditQuestSave_Click(object sender, MVControlEventArgs e)
        {
        }

        [MVControlEvent("StartStop", "Click")]
        void QuestRefresh_Click(object sender, MVControlEventArgs e)
        {
            run = !run;
            var type = run ? "Started" : "Stopped";
            Util.WriteToChat($"Execution {type}");
        }

        [MVControlEvent("Step", "Click")]
        void Stop_Click(object sender, MVControlEventArgs e)
        {
            Step();
        }

        [MVControlEvent("ScramSort", "Click")]
        void QuestTick_Click(object sender, MVControlEventArgs e)
        {
            sort = !sort;
            var type = sort ? "Sort" : "Scramble";

            //var core = Decal.Adapter.CoreManager.Current;
            //var y = core.WorldFilter.GetByOwner(core.CharacterFilter.Id);
            //var pretty = y.Select(x => $"{x.Name} -- {x.Id} -- {x.Type} -- {x.Container} -- {x.Values(LongValueKey.Slot, -1)}");

            //var z = pretty.Aggregate((total, next) => $"{total},\n{next}");
            //Util.LogData(JsonConvert.SerializeObject(y.Select(x => new Item(x)), Formatting.Indented));

            Util.WriteToChat($"Set to {type}");
        }

        void Step()
        {
            if (sort) SortInv();
            else Scramble();
        }

        public void SortInv()
        {
            var core = CoreManager.Current;
            var inventory = core.WorldFilter.GetByOwner(core.CharacterFilter.Id).Select(x => new Item(x));

            var action = BagManager.GetNextAction(inventory, core.CharacterFilter.Id);

            if (action != null)
                Host.Actions.MoveItem(action.ObjectId, action.PackId, action.Slot, true);
        }

        void Scramble()
        {
            var random = new Random();
            var core = CoreManager.Current;
            var item = core.WorldFilter.GetByContainer(core.CharacterFilter.Id)
                .OrderBy(x => random.Next())
                .Where(x => x.ObjectClass != ObjectClass.Container && x.Values(LongValueKey.Slot, -1) != -1)
                .First();
            Host.Actions.MoveItem(item.Id, core.CharacterFilter.Id, 0, false);
        }
    }
}
