using System;
using System.IO;
using System.Collections.Generic;
using CsvHelper;
using System.IO;
using System.Formats.Asn1;
using System.Globalization;
using Microsoft.Win32.SafeHandles;
using System.Text.RegularExpressions;
using System.Net;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Xml;
using Microsoft.VisualBasic;

public class PersonOnServerAtTime
{
    public string Person { get; set; }
    public string Server { get; set; }
    public string Time { get; set; }
}

public class GroupEvent
{
    // constructor that creates the People HashSet object
    public GroupEvent()
    {
        People = new HashSet<string>();
    }
    public HashSet<string> People { get; set; }
    public string Server { get; set; }
    public int StartMinute { get; set; }
    public int EndMinute { get; set; }
    public int Duration { get { return EndMinute - StartMinute; } }


    // Return true if this group existed at any moment within this minute range.
    // Scenarios: # is a range limit, < > is the group lifetime.
    // # < > #
    // # < # >
    // # # < > false
    // < # > #
    // < > # # false
    // < # # >

}

public class FindPatterns
{
    // do I see these people in any group at any time in this range?
    public static GroupEvent? AssembledAnyTimeInRange(List<GroupEvent> groups, HashSet<string> people, int rangeStart, int rangeDuration)
    {
        int rangeEnd = rangeStart + rangeDuration;

        foreach (var group in groups)
        {
            bool fInRange = false;
            // # < > #
            // # < # >
            if (group.StartMinute > rangeStart)
                if (group.StartMinute < rangeEnd)
                    fInRange = true;

            // < # > #
            // < # # >
            if (group.StartMinute < rangeStart)
                if (group.EndMinute > rangeStart)
                    fInRange = true;

            if (false == fInRange)
                continue;

            //            bool fMatch = group.People.SetEquals(people);

            // the group event is in the duration range. Does it contain these people?
            // EVEN A 50%+ MATCH IS ADEQUATE
            int iPeopleMatched = 0;
            foreach (var person in people)
            {
                if (group.People.Contains(person))
                    iPeopleMatched++;
            }

            int iGroupMatched = 0;
            foreach (var person in group.People)
            {
                if (people.Contains(person))
                    iGroupMatched++;
            }

            if (people.Count / 2 < iPeopleMatched)
                return group;
            if (group.People.Count / 2 < iGroupMatched)
                return group;

            return null;
        }
        return null;
    }

    public static GroupEvent? AssembledAnyTimeRange(HashSet<GroupEvent> groupaGroupEvents, int rangeStart, int rangeDuration)
    {
        int rangeEnd = rangeStart + rangeDuration;
        foreach (var group in groupaGroupEvents)
        {
            bool fInRange = false;
            // # < > #
            // # < # >
            if (group.StartMinute > rangeStart)
                if (group.StartMinute < rangeEnd)
                    fInRange = true;

            // < # > #
            // < # # >
            if (group.StartMinute < rangeStart)
                if (group.EndMinute > rangeStart)
                    fInRange = true;

            if (false == fInRange)
                continue;

            return group;
        }
        return null;
    }



    public static bool AssembledNow(List<GroupEvent> groups, GroupEvent group)
    {
        return false;
    }


    public static GroupEvent GroupInGroups(GroupEvent group, List<GroupEvent> dedupedGroups)
    {
        foreach (var g in dedupedGroups)
        {
            if (g.People.SetEquals(group.People))
                return g;
        }
        return null;
    }

    public static int MinuteSince2023AsInt()
    {
        var now = DateTime.Now;
        var then = new DateTime(2023, 1, 1);
        var diff = now - then;
        // Format ToString to just show an int.

        int mins = (int)(diff.TotalMinutes);
        return mins + 420;
    }



    //     static bool fNamesLoaded = false;
    // static Object nameMap = null;
    static KeyValuePair<string, string>[] NameMap()
    {
        //        if (false == fNamesLoaded)
        //        {
        string s = System.IO.File.ReadAllText(@"c:\users\user\guidnamepairs.json");
        return JsonSerializer.Deserialize<KeyValuePair<string, string>[]>(s);
        //     fNamesLoaded = true;
        //        }
        //        return nameMap;
    }



    public static void WeSaw(GroupEvent ge)
    {
        Console.WriteLine(ge.People.Count + " musicians on " + ge.Server + " for " + ge.Duration + " minutes starting at " + ge.StartMinute);
        foreach (var str in ge.People)
        {
            string name = "";
            foreach (var item in NameMap())
            {
                if (item.Key == str)
                {
                    name = item.Value;
                    break;
                }
            }
            if (name.Length > 0)
                Console.WriteLine("   " + name);
            else
                Console.WriteLine("        " + str);
        }
        Console.WriteLine();
    }

    static bool LooseMemberMatch(GroupEvent ge1, GroupEvent ge2)
    {
        int ige1Matched = 0;
        foreach (var person in ge1.People)
        {
            if (ge2.People.Contains(person))
                ige1Matched++;
        }

        int ige2Matched = 0;
        foreach (var person in ge2.People)
        {
            if (ge1.People.Contains(person))
                ige2Matched++;
        }

        if (ge1.People.Count / 2 < ige1Matched)
            return true;
        if (ge2.People.Count / 2 < ige2Matched)
            return true;

        return false;

    }



    public static void Main()
    {
        var dedupedGroups = new List<GroupEvent>();

        // if the file exists, don't re-load
        const string RAW_DATA_FILE = @"c:\users\user\raw.json";
        while (true)
        {
            if (false == System.IO.File.Exists(RAW_DATA_FILE))
            {
                // Create a reader object and read the CSV file into a list of objects
                var reader = new StreamReader("c:\\users\\user\\trimmed_census_6.csv");

                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<PersonOnServerAtTime>().ToList();

                    // Consider NOW to be the highest minute recorded in the data.
                    records.Sort((x, y) => x.Time.CompareTo(y.Time));
                    int startOfSample = Int32.Parse(records[0].Time);

                    records.Sort((x, y) => y.Time.CompareTo(x.Time));
                    int endOfSample = Int32.Parse(records[0].Time);

                    // Now create GroupTogether objects
                    var groups = new List<GroupEvent>();

                    // Sort by server, then by minute

                    records.Sort((x, y) => x.Server.CompareTo(y.Server));
                    records.Sort((x, y) => x.Time.CompareTo(y.Time));

                    // all GroupEvents begin on the first minute they're noticed, as one-minute events.
                    // Then if they still exist in the next minute, their EndMinute rises.
                    // This ends when the People lose at least one member.

                    // The quick-dirty way to start this is a dictionary of time+server keys, filled by people.
                    Dictionary<string, HashSet<string>> protoClique = new Dictionary<string, HashSet<string>>();
                    foreach (var record in records)
                    {
                        string key = record.Time + record.Server;
                        if (protoClique.ContainsKey(key))
                        {
                            protoClique[key].Add(record.Person);
                        }
                        else
                        {
                            protoClique.Add(key, new HashSet<string>() { record.Person });
                        }
                    }

                    // Finally, create GroupEvents based on these one-minute cliques.

                    foreach (var key in protoClique.Keys)
                    {
                        var group = new GroupEvent();
                        group.Server = key.Substring(6);
                        group.StartMinute = Int32.Parse(key.Substring(0, 6));
                        group.EndMinute = Int32.Parse(key.Substring(0, 6)); // so zero duration at start, extended in next loop
                        group.People = protoClique[key];
                        groups.Add(group);
                    }

                    // ok, for each group in groups, extend its endminute as long as the people remain

                    foreach (var group in groups)
                    {
                        bool stillTogether = true;
                        while (stillTogether)
                        {
                            int nextMinute = group.EndMinute + 1;
                            string nextKey = nextMinute.ToString() + group.Server;
                            if (protoClique.ContainsKey(nextKey))
                            {
                                // check if all the people in the group are in the next minute's clique
                                // It's ok if noobs appear? Yes. 
                                foreach (var person in group.People)
                                {
                                    if (false == protoClique[nextKey].Contains(person))
                                    {
                                        stillTogether = false;
                                    }
                                }
                                if (stillTogether)
                                {
                                    group.EndMinute = nextMinute;
                                }
                            }
                            else
                            {
                                stillTogether = false;
                            }
                        }
                    }

                    // Now tell me about the big groups that had long runs.
                    // Sort by group size times duration (in minutes)
                    // And let's dump the smaller, shorter groups.

                    groups.Sort((x, y) => (y.People.Count * y.Duration).CompareTo(x.People.Count * x.Duration));
                    var topGroups = new List<GroupEvent>();
                    foreach (var group in groups)
                    {
                        if (group.People.Count > 2)
                            if (group.Duration > 10)
                            {
                                Console.WriteLine("Server: " + group.Server + " Start: " + group.StartMinute + " Size: " + group.People.Count + " Duration: " + (group.Duration));
                                topGroups.Add(group);
                            }
                    }
                    groups = topGroups;
                    topGroups = null;

                    // For each group, see if I've seen this group before in a time range that crosses over this one.

                    dedupedGroups.Clear(); // just so we know this isn't used until here.

                    foreach (var group in groups)
                    {
                        GroupEvent alreadyFoundGroup = null;
                        alreadyFoundGroup = GroupInGroups(group, dedupedGroups);
                        if (null == alreadyFoundGroup)
                        {
                            dedupedGroups.Add(group);
                        }
                        else
                        {
                            bool fInRange = false;
                            if (group.StartMinute > alreadyFoundGroup.StartMinute)
                                if (group.StartMinute < alreadyFoundGroup.EndMinute)
                                    fInRange = true;
                            if (group.StartMinute < alreadyFoundGroup.StartMinute)
                                if (group.EndMinute > alreadyFoundGroup.EndMinute)
                                    fInRange = true;

                            if (fInRange)
                            {
                                if (group.Duration > alreadyFoundGroup.Duration)
                                {
                                    dedupedGroups.Remove(alreadyFoundGroup);
                                    dedupedGroups.Add(group);
                                }
                                else
                                {
                                    // do nothing, because we're keeping the fat one in deduped.
                                }
                            }
                        }
                    }
                    groups = null;

                    // I guess sort by size? Or recency?
                    // Let recency kick things off?
                    // so sort by end time?
                    // or by size? size is gonna be kinda key.
                    // patterns in very large groups are the strongest signals.
                    // so sort first by size, then by recency?
                    // BUT I CAN REMOVE OLD DATA IN THE SOURCE SO HERE I JUST FIND HUGE GATHERINGS
                    // AND CONNECT THEM TO SMALLER SUBSETS SEEN AT ANY TIME.
                    // AND ALL REACH A SET THAT TRIES TO PREDICT WHERE THEY'LL MATERIALIZE
                    // AND SUMMARIZE WHO THEY ARE ETC.
                    // counts, sorted by duration
                    //                dedupedGroups.Sort((x, y) => (y.Duration).CompareTo(x.Duration));
                    //                dedupedGroups.Sort((x, y) => (y.People.Count).CompareTo(x.People.Count));
                    dedupedGroups.Sort((x, y) =>
                    {
                        int countCompare = y.People.Count.CompareTo(x.People.Count);
                        if (countCompare != 0)
                            return countCompare;
                        return y.Duration.CompareTo(x.Duration);
                    });

                    string jsonString = JsonSerializer.Serialize(dedupedGroups);
                    System.IO.File.WriteAllText(RAW_DATA_FILE, jsonString);
                }
            }

            var f = System.IO.File.ReadAllText(RAW_DATA_FILE);
            dedupedGroups = System.Text.Json.JsonSerializer.Deserialize<List<GroupEvent>>(f);

            HashSet<HashSet<GroupEvent>> bunchedGroupings = new HashSet<HashSet<GroupEvent>>();

            // Now group the groupEvents by membership
            List<GroupEvent> listOfGroupEvents = dedupedGroups.ToList<GroupEvent>();
            listOfGroupEvents.Reverse();
            for (int iPos = listOfGroupEvents.Count - 1; iPos >= 0; iPos--)
            {
                var thisEvent = listOfGroupEvents[iPos];
                listOfGroupEvents.RemoveAt(iPos);
                iPos--;
                var thisGroupOfGroups = new HashSet<GroupEvent> { thisEvent };
                bunchedGroupings.Add(thisGroupOfGroups);

                // for each loose match to thisEvent, move item from one list to the other
                for (int iSecondaryPos = iPos - 1; iSecondaryPos >= 0; iSecondaryPos--)
                {
                    var compareEvent = listOfGroupEvents[iSecondaryPos];
                    if (LooseMemberMatch(thisEvent, compareEvent))
                    {
                        listOfGroupEvents.RemoveAt(iSecondaryPos);
                        iPos--;
                        iSecondaryPos--;
                        thisGroupOfGroups.Add(compareEvent);
                    }
                }
            }


            Console.WriteLine("Current time: " + MinuteSince2023AsInt().ToString());

            List<GroupEvent> chosen = new List<GroupEvent>();

            foreach (var groupaGroupEvents in bunchedGroupings)
            {
                const int FULL_DAY = 60 * 24;
                const int FULL_WEEK = FULL_DAY * 7;

                var match1 = AssembledAnyTimeRange(groupaGroupEvents, MinuteSince2023AsInt() - FULL_WEEK, 360);
                if (null != match1)
                {
                    var match2 = AssembledAnyTimeRange(groupaGroupEvents, MinuteSince2023AsInt() - 2 * FULL_WEEK, 360);
                    if (null != match2)
                    {
                        var match3 = AssembledAnyTimeRange(groupaGroupEvents, MinuteSince2023AsInt() - 3 * FULL_WEEK, 360);
                        if (null != match3)
                        {
                            //                        var match4 = AssembledAnyTimeRange(groupaGroupEvents, MinuteSince2023AsInt() - 4 * FULL_WEEK, 120);
                            //                        if (null != match4)
                            {
                                //                    foreach (GroupEvent groupEvent in groupaGroupEvents)
                                {
                                    // has match1's start time last week already passed this week?

                                    int now = MinuteSince2023AsInt();
                                    if (match1.StartMinute + FULL_WEEK < now)
                                        Console.WriteLine("The first one has already probably started. Don't hype it.");
                                    else
                                    {
                                        WeSaw(match1);
                                        Console.WriteLine("----------------------------");
                                        WeSaw(match2);
                                        Console.WriteLine("Deviation from most recent start: " + (match2.StartMinute + FULL_WEEK - match1.StartMinute).ToString());
                                        Console.WriteLine("----------------------------");
                                        WeSaw(match3);
                                        Console.WriteLine("Deviation from most recent start: " + (match3.StartMinute + FULL_WEEK * 2 - match1.StartMinute).ToString());
                                        Console.WriteLine("============================");

                                        GroupEvent geHybrid = new GroupEvent();

                                        // Determine who appears in all 3
                                        foreach (var p1 in match1.People)
                                        {
                                            if (geHybrid.People.Contains(p1))
                                                continue;   // so loggin in twice does nuttin

                                            if (match2.People.Contains(p1))
                                                if (match3.People.Contains(p1))
                                                    geHybrid.People.Add(p1);
                                        }

                                        // if there weren't three-matches, try two-matches with the most recent
                                        if (geHybrid.People.Count == 0)
                                        {
                                            // Determine who appears in all 3
                                            foreach (var p1 in match1.People)
                                            {
                                                if (geHybrid.People.Contains(p1))
                                                    continue;   // so loggin in twice does nuttin

                                                if (match2.People.Contains(p1) || match3.People.Contains(p1))
                                                    geHybrid.People.Add(p1);
                                            }
                                        }

                                        if (match1.Server == match2.Server)
                                            if (match3.Server == match1.Server)
                                                geHybrid.Server = match1.Server;

                                        // say it starts the time the last one started
                                        geHybrid.StartMinute = match1.StartMinute + FULL_WEEK;

                                        var avgDur = (match1.Duration + match2.Duration + match3.Duration) / 3;
                                        geHybrid.EndMinute = geHybrid.StartMinute + avgDur;

                                        // only add if there's no crossover with existing chosens.
                                        bool fAddIt = true;
                                        foreach (var ge in chosen)
                                        {
                                            if (LooseMemberMatch(ge, geHybrid))
                                            {
                                                fAddIt = false;
                                            }
                                        }

                                        if (fAddIt)
                                            chosen.Add(geHybrid);
                                        //                                        Console.WriteLine(JsonSerializer.Serialize(geHybrid));
                                        //                                        Console.WriteLine("============================");

                                    }
                                }
                            }
                        }
                    }
                }
            }

            var jsonStringPredicted = JsonSerializer.Serialize(chosen);
            System.IO.File.WriteAllText("predicted.json", jsonStringPredicted);
            Console.WriteLine(jsonStringPredicted);

            Thread.Sleep(1000 * 60 * 10);
        }
    }
}

