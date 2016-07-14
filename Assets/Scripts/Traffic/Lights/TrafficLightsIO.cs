// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: This file contains the data structures and methods to parse
//          Vissim's traffic light data files which are XML files.

/* Not Used

using System.Collections.Generic;
using System.Xml;

public struct SignalDisplay
{
    public int id;
    public string name;
}

public struct SignalSequence
{
    public struct State
    {
        public int displayID;
        public int defaultDuration;
        public bool isFixedDuration;
        public bool isClosed;
    }

    public int id;
    public string name;
    public State[] states;
}

public struct SignalGroup
{
    public int id;
    public string name;
    public int defaultSignalSequenceID;
    public Dictionary<int, int> defaultDurations;
}

public static class TrafficLightsIO
{
    public static void Load(string filename)
    {
        Dictionary<int, SignalDisplay> signalDisplays = new Dictionary<int, SignalDisplay>();
        Dictionary<int, SignalSequence> signalSequences = new Dictionary<int, SignalSequence>();
        Dictionary<int, SignalGroup> signalGroups = new Dictionary<int, SignalGroup>();

        var xml = new XmlDocument();
        xml.Load(filename);

        XmlNode root = xml.DocumentElement;
        foreach (XmlNode node in root.ChildNodes)
        {
            switch (node.Name)
            {
                case "signaldisplays":
                    ParseSignalDisplays(node, signalDisplays);
                    break;
                case "signalsequences":
                    ParseSignalSequences(node, signalSequences);
                    break;
                case "sgs":
                    ParseSignalGroups(node, signalGroups);
                    break;
                case "progs":
                    //ParsePrograms(node, programs);
                    break;
            }
        }
    }

    private static void ParseSignalDisplays(XmlNode parent, Dictionary<int, SignalDisplay> signalDisplays)
    {
        foreach (XmlNode node in parent.ChildNodes)
        {
            int id = int.Parse(node.Attributes["id"].Value);
            signalDisplays.Add(id, new SignalDisplay
            {
                id = id,
                name = node.Attributes["name"].Value
            });
        }
    }

    private static void ParseSignalSequences(XmlNode parent, Dictionary<int, SignalSequence> signalSequences)
    {
        foreach (XmlNode node in parent.ChildNodes)
        {
            List<SignalSequence.State> states = new List<SignalSequence.State>();
            foreach (XmlNode state in node.ChildNodes)
            {
                states.Add(new SignalSequence.State {
                    displayID = int.Parse(state.Attributes["display"].Value),
                    defaultDuration = int.Parse(state.Attributes["defaultDuration"].Value),
                    isFixedDuration = state.Attributes["isFixedDuration"].Value == "true",
                    isClosed = state.Attributes["isClosed"].Value == "true"
                });
            }

            int id = int.Parse(node.Attributes["id"].Value);
            signalSequences.Add(id, new SignalSequence
            {
                id = id,
                name = node.Attributes["name"].Value,
                states = states.ToArray()
            });
        }
    }

    private static void ParseSignalGroups(XmlNode parent, Dictionary<int, SignalGroup> signalGroups)
    {
        foreach (XmlNode node in parent.ChildNodes)
        {
            Dictionary<int, int> defaultDurations = new Dictionary<int, int>();
            XmlNode defaultDurationsNode = node.SelectSingleNode("defaultDurations");
            foreach (XmlNode duration in defaultDurationsNode.ChildNodes)
            {
                defaultDurations.Add(
                    int.Parse(duration.Attributes["display"].Value),
                    int.Parse(duration.Attributes["duration"].Value));
            }

            int id = int.Parse(node.Attributes["id"].Value);
            signalGroups.Add(id, new SignalGroup
            {
                id = id,
                name = node.Attributes["name"].Value,
                defaultSignalSequenceID = int.Parse(node.Attributes["defaultSignalSequence"].Value),
                defaultDurations = defaultDurations
            });
        }
    }

}
*/
