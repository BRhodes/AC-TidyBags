using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;
using System;
using System.IO;
using System.Linq;
using System.Timers;
using TidyBags.Models;
using VirindiViewService;

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
        double delayBeforeStartingWork = 3;
        double movesPerSecond = 8;

        public long LastHashChange { get; private set; }

        long LastHash { get; set; } = 0;
        long LastStep { get; set; } = 0;
        bool Run { get; set; } = true;
        bool Running { get; set; } = false;

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
                //foreach (var view in views)
                //{
                //    if (view.Title == "Quest Helper")
                //    {
                //        //questView = view;
                //    }
                //}

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
            var stepCooldown = TimeSpan.FromSeconds(1.0 / movesPerSecond).Ticks;
            if (Run || Running)
            {
                if (!Running)
                {
                    var hash = Util.ComputeInventoryHash(Core.WorldFilter.GetInventory());
                    if (hash != LastHash)
                    {
                        LastHashChange = DateTime.UtcNow.Ticks;
                        LastHash = hash;
                    }
                    if (LastHashChange < DateTime.UtcNow.Ticks - TimeSpan.FromSeconds(delayBeforeStartingWork).Ticks)
                    {
                        Running = true;
                    }
                }
                else if (Running && LastStep + stepCooldown < DateTime.UtcNow.Ticks)
                {
                    LastStep = DateTime.UtcNow.Ticks;
                    var action = Step();
                    if (action == null)
                    {
                        LastHash = Util.ComputeInventoryHash(Core.WorldFilter.GetInventory());
                        LastHashChange = DateTime.MaxValue.Ticks;
                        Running = false;
                    }
                }
            }
        }


        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
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
            Run = !Run;
            var type = Run ? "Started" : "Stopped";
            Util.WriteToChat($"Execution {type}");
        }

        [MVControlEvent("Step", "Click")]
        void Stop_Click(object sender, MVControlEventArgs e)
        {
            Step();
        }

        [MVControlEvent("SpeedUp", "Click")]
        void Faster(object sender, MVControlEventArgs e)
        {
        }

        [MVControlEvent("SpeedDown", "Click")]
        void Slower(object sender, MVControlEventArgs e)
        {
        }

        [MVControlEvent("ScramSort", "Click")]
        void QuestTick_Click(object sender, MVControlEventArgs e)
        {
        }

        MoveItemAction Step()
        {
            var core = CoreManager.Current;
            var inventory = core.WorldFilter.GetByOwner(core.CharacterFilter.Id).Select(x => new Item(x));

            var action = BagManager.GetNextAction(inventory, core.CharacterFilter.Id);

            if (action != null)
                Host.Actions.MoveItem(action.ObjectId, action.PackId, action.Slot, true);

            return action;
        }
    }
}
