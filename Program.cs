﻿#define WINDOWS

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
using System.Runtime.CompilerServices;
// using Renci.SshNet;
using System.Resources;
// using WinSCP;
using System.Security.Cryptography;
using Renci.SshNet;

public class PersonOnServerAtTime
{
    public string Person { get; set; }
    public string Server { get; set; }
    public string Time { get; set; }
}

public class BaseGroupEvent
{
    // constructor that creates the People HashSet object
    public string ServerIpPort { get; set; }
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

public class InternalGroupEvent : BaseGroupEvent
{
    public InternalGroupEvent()
    {
        PeopleGuids = new HashSet<string>();
    }

    public HashSet<string> PeopleGuids { get; set; }

}

public class MusicianMetadata
{
    public string Guid { get; set; }
    public string Name { get; set; }
    public string Instrument { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}

public class FriendlyGroupEvent : BaseGroupEvent
{
    public HashSet<MusicianMetadata> People { get; set; }
    public int MinutesUntil { get { return StartMinute - FindPatterns.MinuteSince2023AsInt();  } }
    public string ServerName { get; set; }
    public string ServerCity { get; set; }
    public string ServerCountry { get; set; }

}

public class ServerMetadata
{
    public string IpPort { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}



public class FindPatterns
{
    // do I see these people in any group at any time in this range?
    public static InternalGroupEvent? AssembledAnyTimeInRange(List<InternalGroupEvent> groups, HashSet<string> people, int rangeStart, int rangeDuration)
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
                if (group.PeopleGuids.Contains(person))
                    iPeopleMatched++;
            }

            int iGroupMatched = 0;
            foreach (var person in group.PeopleGuids)
            {
                if (people.Contains(person))
                    iGroupMatched++;
            }

            if (people.Count / 2 < iPeopleMatched)
                return group;
            if (group.PeopleGuids.Count / 2 < iGroupMatched)
                return group;

            return null;
        }
        return null;
    }

    public static InternalGroupEvent? AssembledAnyTimeRange(HashSet<InternalGroupEvent> groupaGroupEvents, int rangeStart, int rangeDuration)
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



    public static bool AssembledNow(List<InternalGroupEvent> groups, InternalGroupEvent group)
    {
        return false;
    }


    public static InternalGroupEvent GroupInGroups(InternalGroupEvent group, List<InternalGroupEvent> dedupedGroups)
    {
        foreach (var g in dedupedGroups)
        {
            if (g.PeopleGuids.SetEquals(group.PeopleGuids))
                return g;
        }
        return null;
    }

    public static int MinuteSince2023AsInt()
    {
        // var now = DateTime.Now;
        var now = DateTime.UtcNow;
        // var then = new DateTime(2023, 1, 1);
        var then = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var diff = now - then;
        // Format ToString to just show an int.

        int mins = (int)(diff.TotalMinutes);
//#if WINDOWS
//        mins += 420;
//#endif
        return mins;
    }

    // first time, load data.
    // from then on, use loaded data.
    static bool gfLoaded = false;
    static List<MusicianMetadata> allMusicians = null;
    static string? NameMetadata(string guid)
    {
        if (false == gfLoaded)
        {
            var reader = new StreamReader("census_metadata.csv");

            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                allMusicians = csv.GetRecords<MusicianMetadata>().ToList();
            }
            gfLoaded = true;
        }

        foreach (var musician in allMusicians)
            if (musician.Guid == guid)
                return musician.Name;
        return null;
    }

    static string? InstrumentMetadata(string guid)
    {
        foreach (var musician in allMusicians)

            if (musician.Guid == guid)
            {
                if (musician.Instrument == "-")
                    return "";
                else
                    return musician.Instrument;
            }
        return null;
    }

    static string? CityMetadata(string guid)
    {
        foreach (var musician in allMusicians)
            if (musician.Guid == guid)
                return musician.City;
        return null;
    }

    static string? CountryMetadata(string guid)
    {
        foreach (var musician in allMusicians)
            if (musician.Guid == guid)
            {
                if (musician.Country == "-")
                    return "";
                return musician.Country;
            }
        return null;
    }



    static bool gfServerMetadataLoaded = false;
    static List<ServerMetadata> allServerMetadata = null;

    static void LoadUp()
    {
        if (false == gfServerMetadataLoaded)
        {
            var reader = new StreamReader("servers_metadata.csv");

            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                allServerMetadata = csv.GetRecords<ServerMetadata>().ToList();
            }
            gfServerMetadataLoaded = true;
        }
    }


    static string ServerNameMetadata(string ipPort)
    {
        if (ipPort == null)
            return "";

        LoadUp();

        foreach (var server in allServerMetadata)
            if (server.IpPort == ipPort)
                return server.Name;
        return "";

    }

    static string ServerCityMetadata(string ipPort)
    {
        LoadUp();

        foreach (var server in allServerMetadata)
            if (server.IpPort == ipPort)
                return server.City;
        return "";
    }

    static string ServerCountryMetadata(string ipPort)
    {
        foreach (var server in allServerMetadata)
            if (server.IpPort == ipPort)
                return server.Country;
        return "";
    }

    public static void WeSaw(InternalGroupEvent ge)
    {
        Console.WriteLine(ge.PeopleGuids.Count + " musicians on " + ge.ServerIpPort + " for " + ge.Duration + " minutes starting at " + ge.StartMinute);
        foreach (var guid in ge.PeopleGuids)
        {
            Console.Write("   " + NameMetadata(guid));
        }
        Console.WriteLine();
    }

    static bool LooseMemberMatch(InternalGroupEvent ge1, InternalGroupEvent ge2)
    {
        int ige1Matched = 0;
        foreach (var person in ge1.PeopleGuids)
        {
            if (ge2.PeopleGuids.Contains(person))
                ige1Matched++;
        }

        int ige2Matched = 0;
        foreach (var person in ge2.PeopleGuids)
        {
            if (ge1.PeopleGuids.Contains(person))
                ige2Matched++;
        }

        if (ge1.PeopleGuids.Count / 2 < ige1Matched)
            return true;
        if (ge2.PeopleGuids.Count / 2 < ige2Matched)
            return true;

        return false;

    }


    static string TuccUsFromFile()
    {
        var reader = new StreamReader("tucc.us");
        return reader.ReadToEnd();
    }


    public static void Main(string[] args)
    {
        var dedupedGroups = new List<InternalGroupEvent>();

        // if the file exists, don't re-load
        const string RAW_DATA_FILE = "cooked.json";
        while (true)
        {
            bool fRecook = false;

            if (false == System.IO.File.Exists(RAW_DATA_FILE))
                fRecook = true;

            if (fRecook)
            {
                // I'm gonna grab fresh remote files to local copies.
                // shove.bat does this.
                //DownloadFileAsync("https://jamulus.live/census_uniq.csv", "census_uniq.csv");
                //DownloadFileAsync("https://jamulus.live/census_metadata.csv", "census_metadata.csv");
                //DownloadFileAsync("https://jamulus.live/servers_metadata.csv", "servers_metadata.csv");

                // Create a reader object and read the CSV file into a list of objects
                var reader = new StreamReader("census_uniq.csv");

                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<PersonOnServerAtTime>().ToList();

                    // Consider NOW to be the highest minute recorded in the data.
                    records.Sort((x, y) => x.Time.CompareTo(y.Time));
                    int startOfSample = Int32.Parse(records[0].Time);

                    records.Sort((x, y) => y.Time.CompareTo(x.Time));
                    int endOfSample = Int32.Parse(records[0].Time);

                    // Now create GroupTogether objects
                    var groups = new List<InternalGroupEvent>();

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
                        var group = new InternalGroupEvent();
                        group.ServerIpPort = key.Substring(7);
                        group.StartMinute = Int32.Parse(key.Substring(0, 7));
                        group.EndMinute = Int32.Parse(key.Substring(0, 7)); // so zero duration at start, extended in next loop
                        group.PeopleGuids = protoClique[key];
                        groups.Add(group);
                    }

                    // ok, for each group in groups, extend its endminute as long as the people remain

                    foreach (var group in groups)
                    {
                        bool stillTogether = true;
                        while (stillTogether)
                        {
                            int nextMinute = group.EndMinute + 1;
                            string nextKey = nextMinute.ToString("D7") + group.ServerIpPort;
                            if (protoClique.ContainsKey(nextKey))
                            {
                                // check if all the people in the group are in the next minute's clique
                                // It's ok if noobs appear? Yes. 
                                foreach (var person in group.PeopleGuids)
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

                    groups.Sort((x, y) => (y.PeopleGuids.Count * y.Duration).CompareTo(x.PeopleGuids.Count * x.Duration));
                    var topGroups = new List<InternalGroupEvent>();
                    foreach (var group in groups)
                    {
                        if (group.PeopleGuids.Count > 2)
                            if (group.Duration > 10)
                            {
                                Console.WriteLine("Server: " + group.ServerIpPort + " Start: " + group.StartMinute + " Size: " + group.PeopleGuids.Count + " Duration: " + (group.Duration));
                                topGroups.Add(group);
                            }
                    }
                    groups = topGroups;
                    topGroups = null;

                    // For each group, see if I've seen this group before in a time range that crosses over this one.

                    dedupedGroups.Clear(); // just so we know this isn't used until here.

                    foreach (var group in groups)
                    {
                        InternalGroupEvent alreadyFoundGroup = null;
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
                        int countCompare = y.PeopleGuids.Count.CompareTo(x.PeopleGuids.Count);
                        if (countCompare != 0)
                            return countCompare;
                        return y.Duration.CompareTo(x.Duration);

                    });

                    string jsonString = JsonSerializer.Serialize(dedupedGroups);
                    System.IO.File.WriteAllText(RAW_DATA_FILE, jsonString);
                }
            }

            var f = System.IO.File.ReadAllText(RAW_DATA_FILE);
            dedupedGroups = System.Text.Json.JsonSerializer.Deserialize<List<InternalGroupEvent>>(f);

            HashSet<HashSet<InternalGroupEvent>> bunchedGroupings = new HashSet<HashSet<InternalGroupEvent>>();

            // Now group the groupEvents by membership
            List<InternalGroupEvent> listOfGroupEvents = dedupedGroups.ToList<InternalGroupEvent>();
            listOfGroupEvents.Reverse();
            for (int iPos = listOfGroupEvents.Count - 1; iPos >= 0; iPos--)
            {
                var thisEvent = listOfGroupEvents[iPos];
                listOfGroupEvents.RemoveAt(iPos);
                iPos--;
                var thisGroupOfGroups = new HashSet<InternalGroupEvent> { thisEvent };
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

            List<InternalGroupEvent> chosen = new List<InternalGroupEvent>();

            foreach (var groupaGroupEvents in bunchedGroupings)
            {
                const int FULL_DAY = 60 * 24;
                const int FULL_WEEK = FULL_DAY * 7;

                var match1 = AssembledAnyTimeRange(groupaGroupEvents, MinuteSince2023AsInt() - FULL_WEEK, (FULL_DAY / 2));
                if (null != match1)
                {
                    var match2 = AssembledAnyTimeRange(groupaGroupEvents, MinuteSince2023AsInt() - 2 * FULL_WEEK, (FULL_DAY / 2));
                    if (null != match2)
                    {
                        var match3 = AssembledAnyTimeRange(groupaGroupEvents, MinuteSince2023AsInt() - 3 * FULL_WEEK, (FULL_DAY / 2));
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

                                        InternalGroupEvent geHybrid = new InternalGroupEvent();

                                        // Determine who appears in all 3
                                        foreach (var p1 in match1.PeopleGuids)
                                        {
                                            if (geHybrid.PeopleGuids.Contains(p1))
                                                continue;   // so loggin in twice does nuttin

                                            if (match2.PeopleGuids.Contains(p1))
                                                if (match3.PeopleGuids.Contains(p1))
                                                    geHybrid.PeopleGuids.Add(p1);
                                        }

                                        // if there weren't three-matches, try two-matches with the most recent
                                        if (geHybrid.PeopleGuids.Count == 0)
                                        {
                                            // Determine who appears in all 3
                                            foreach (var p1 in match1.PeopleGuids)
                                            {
                                                if (geHybrid.PeopleGuids.Contains(p1))
                                                    continue;   // so loggin in twice does nuttin

                                                if (match2.PeopleGuids.Contains(p1) || match3.PeopleGuids.Contains(p1))
                                                    geHybrid.PeopleGuids.Add(p1);
                                            }
                                        }

                                        if (match1.ServerIpPort == match2.ServerIpPort)
                                            if (match3.ServerIpPort == match1.ServerIpPort)
                                                geHybrid.ServerIpPort = match1.ServerIpPort;

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
                                        {
                                            int iPeopleVisibleNow = 0;
                                            // ok, I don't show sets where more than half the people are already here.
                                            foreach (var personGuid in geHybrid.PeopleGuids)
                                            {
                                                if (ExtractInsights.VisibleNow.UserVisibleNow(personGuid))
                                                    iPeopleVisibleNow++;
                                            }

                                            if (iPeopleVisibleNow < geHybrid.PeopleGuids.Count / 2)
                                            {
                                                chosen.Add(geHybrid);
                                            }
                                            else
                                                Console.WriteLine("too many are alreay present. not showin.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // ok, chosen needs to be dereferenced for names, and intrumstnet and city and country need to appear

            List<FriendlyGroupEvent> friendlyEvents = new List<FriendlyGroupEvent>();

            foreach (var futurejam in chosen)
            {
                FriendlyGroupEvent fge = new FriendlyGroupEvent();
                fge.StartMinute = futurejam.StartMinute;
                fge.EndMinute = futurejam.EndMinute;
                fge.ServerIpPort = futurejam.ServerIpPort;
                fge.ServerName = ServerNameMetadata(futurejam.ServerIpPort);
                fge.ServerCity = ServerCityMetadata(futurejam.ServerIpPort);
                fge.ServerCountry = ServerCountryMetadata(futurejam.ServerIpPort);
                fge.People = new HashSet<MusicianMetadata>();
                int iNumStreamers = 0;
                foreach (var personGuid in futurejam.PeopleGuids)
                {
                    MusicianMetadata fp = new MusicianMetadata();
                    fp.Guid = personGuid;
                    fp.Name = NameMetadata(personGuid);
                    fp.Instrument = InstrumentMetadata(personGuid);
                    if (fp.Instrument == "Streamer")
                        iNumStreamers++;
                    if (fp.Instrument == "Recorder")
                        iNumStreamers++;
                    fp.City = CityMetadata(personGuid);
                    fp.Country = CountryMetadata(personGuid);
                    fge.People.Add(fp);
                }

                // we only caare if it's not a majority streamers. screw those.
                if (iNumStreamers < futurejam.PeopleGuids.Count / 2)
                {
                    // the more newsworthy, the bigger window of display
                    // # of people * 1 hour + 1 hour for known server + duration
                    if (fge.StartMinute <
                        MinuteSince2023AsInt()
                            + 120 * fge.People.Count
                            + (fge.ServerIpPort == null ? 0 : 60)
                            + fge.Duration)
                        friendlyEvents.Add(fge);
                    else
                        Console.WriteLine("Too far in the future.");
                }
                else
                {
                    Console.WriteLine("Too many streamers.");
                }
            }

            // sort by start time
            friendlyEvents.Sort((x, y) => x.StartMinute.CompareTo(y.StartMinute));

            var jsonStringPredicted = JsonSerializer.Serialize(friendlyEvents);
            try
            {
                System.IO.File.WriteAllText("/var/www/html/predicted.json", jsonStringPredicted);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                Console.WriteLine("Directory not found because debugging on Windows.");
                System.IO.File.WriteAllText("predicted.json", jsonStringPredicted);
            }
            Thread.Sleep(1000 * 60 * 5); // sleep five mins
        }
    }
}
