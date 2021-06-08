using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TidyBags.Models;

namespace TidyBags
{
    public static class Util
    {
        public static void LogData(string data)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Asheron's Call\" + Globals.PluginName + " data.txt", true))
                {
                    writer.WriteLine("============================================================================");
                    writer.WriteLine(data);
                    writer.WriteLine("============================================================================");
                    writer.WriteLine("");
                    writer.Close();
                }
            }
            catch
            {
            }
        }

        public static void LogError(Exception ex)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Asheron's Call\" + Globals.PluginName + " errors.txt", true))
                {
                    writer.WriteLine("============================================================================");
                    writer.WriteLine(DateTime.Now.ToString());
                    writer.WriteLine("Error: " + ex.Message);
                    writer.WriteLine("Source: " + ex.Source);
                    writer.WriteLine("Stack: " + ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        writer.WriteLine("Inner: " + ex.InnerException.Message);
                        writer.WriteLine("Inner Stack: " + ex.InnerException.StackTrace);
                    }
                    writer.WriteLine("============================================================================");
                    writer.WriteLine("");
                    writer.Close();
                }
            }
            catch
            {
            }
        }

        public static void WriteToChat(string message)
        {
            try
            {
                Globals.Host.Actions.AddChatText("<{" + Globals.PluginName + "}>: " + message, 3);
            }
            catch (Exception ex) { LogError(ex); }
        }

        public static string GetFriendlyTimeDifference(int seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return GetFriendlyTimeDifference(ts);
        }

        public static string GetFriendlyTimeDifference(TimeSpan difference)
        {
            string output = "";
            int parts = 0;

            if (difference.Days > 0) parts = 4;
            else if (difference.Hours > 0) parts = 3;
            else if (difference.Minutes > 0) parts = 2;
            else if (difference.Seconds > 0) parts = 1;

            if (parts >= 4) output += $"{difference.Days}d ";
            if (parts >= 3) output += $"{difference.Hours}h ";
            if (parts >= 2) output += $"{difference.Minutes:D2}m ";
            if (parts >= 1) output += $"{difference.Seconds:D2}s ";

            if (output.Length == 0)
                return "Ready";
            return output.Trim();
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        [DllImport("Decal.dll")]
        static extern private int DispatchOnChatCommand(ref IntPtr str, [MarshalAs(UnmanagedType.U4)] int target);

        private static bool Decal_DispatchOnChatCommand(string cmd)
        {
            IntPtr bstr = Marshal.StringToBSTR(cmd);

            try
            {
                bool eaten = (DispatchOnChatCommand(ref bstr, 1) & 0x1) > 0;

                return eaten;
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        /// <summary>
        /// This will first attempt to send the messages to all plugins. If no plugins set e.Eat to true on the message, it will then simply call InvokeChatParser.
        /// </summary>
        /// <param name="cmd"></param>
        public static void DispatchChatToBoxWithPluginIntercept(string cmd)
        {
            if (!Decal_DispatchOnChatCommand(cmd))
                CoreManager.Current.Actions.InvokeChatParser(cmd);
        }

        public static TValue GetValueOrDefault<TKey, TValue>
    (this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default) =>
    dictionary.TryGetValue(key, out var ret) ? ret : defaultValue;

        static public List<Item> LongestIncreasingSubsequence<Item>(IEnumerable<Item> items, Func<Item, int> position)
        {
            var history = new List<List<Item>>();
            foreach (var item in items)
            {
                if (history.Count == 0)
                {
                    history.Add(new List<Item> { item });
                }
                else if (position(item) < position(history[0][0]))
                {
                    history[0][0] = item;
                }
                else
                {
                    for (int i = history.Count - 1; i >= 0; i--) {
                        var story = history[i];
                        if (position(story.Last()) > position(item))
                            continue;

                        var newStory = story.ToList();
                        newStory.Add(item);

                        if (i + 1 < history.Count)
                        {
                            history.RemoveAt(i + 1);
                        }

                        history.Insert(i + 1, newStory);
                        break;
                    }
                }
            }

            return history.Count > 0 ? history.Last() : new List<Item>();
        }

        public static int findLongestConseqSubseq(int[] arr,
                                          int n)
        {
            HashSet<int> S = new HashSet<int>();
            int ans = 0;

            // Hash all the array elements
            for (int i = 0; i < n; ++i)
            {
                S.Add(arr[i]);
            }

            // check each possible sequence from the start
            // then update optimal length
            for (int i = 0; i < n; ++i)
            {
                // if current element is the starting
                // element of a sequence
                if (!S.Contains(arr[i] - 1))
                {
                    // Then check for next elements in the
                    // sequence
                    int j = arr[i];
                    while (S.Contains(j))
                    {
                        j++;
                    }

                    // update  optimal length if this length
                    // is more
                    if (ans < j - arr[i])
                    {
                        ans = j - arr[i];
                    }
                }
            }
            return ans;
        }
    }
}
