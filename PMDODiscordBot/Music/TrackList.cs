// <copyright file="TrackList.cs" company="JordantheBuizel">
// Copyright (c) JordantheBuizel. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Xml;

namespace CSharpDewott.Music
{
    internal class TrackList
    {
        private readonly List<TrackListEntry> entries;

        public List<TrackListEntry> Entries
        {
            get { return this.entries; }
        }

        public TrackList()
        {
            this.entries = new List<TrackListEntry>();
        }

        public void Load(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case "Entry":
                        {
                                using (XmlReader subReader = reader.ReadSubtree())
                                {
                                    this.entries.Add(this.LoadEntry(subReader));
                                }
                            }

                            break;
                    }
                }
            }
        }

        private TrackListEntry LoadEntry(XmlReader reader)
        {
            TrackListEntry entry = new TrackListEntry();
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case "TrackName":
                        {
                                entry.TrackName = reader.ReadString();
                            }

                            break;
                    }
                }
            }

            return entry;
        }
    }
}
