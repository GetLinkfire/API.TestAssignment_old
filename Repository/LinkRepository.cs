﻿using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;

namespace Repository
{
    public class LinkRepository : ILinkRepository
    {
        private readonly Context _context;

        public LinkRepository(Context context)
        {
            _context = context;
        }


        public Link GetLink(Guid linkId)
        {
            var link = _context.Links.Include(x => x.Domain).FirstOrDefault(x => x.Id == linkId && x.IsActive);
            if (link == null)
            {
                throw new Exception($"Link {linkId} not found.");
            }

            return link;
        }

        public Link CreateLink(Link link)
        {
            _context.Domains.Attach(link.Domain);

            if (link.Artists?.Any() == true)
            {
                var linkArtistIds = link.Artists.Select(x => x.Id).ToList();
                var artists = _context.Artists.Where(x => linkArtistIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x);

                foreach (var artist in link.Artists)
                {
                    if (!artists.ContainsKey(artist.Id))
                        artists.Add(artist.Id, artist);
                }

                link.Artists = artists.Select(x => x.Value).ToList();
            }

            link.IsActive = true;
            _context.Links.Add(link);
            _context.SaveChanges();
            return link;
        }

        public Link UpdateLink(Link link)
        {
            var entry = _context.Entry(link);

            // make sure that next fields will be never modified on update
            entry.Property(x => x.MediaType).IsModified = false;
            entry.Property(x => x.IsActive).IsModified = false;

            _context.Domains.Attach(link.Domain);

            // TODO: implement DB link update
            var linkArtistIds = link.Artists.Select(x => x.Id).ToList();
            
            var artists = _context.Artists.Include(x => x.Links)
                                          .Where(x => linkArtistIds.Contains(x.Id))
                                          .ToDictionary(x => x.Id, x => x);

            foreach (var artist in link.Artists.ToList())
            {
                _context.Entry(artist).State = EntityState.Detached;
                if (artists.ContainsKey(artist.Id) && artists[artist.Id].CompareTo(artist) != 0)
                {
                    _context.Entry(artists[artist.Id]).CurrentValues.SetValues(artist);
                }
                else if (!artists.ContainsKey(artist.Id))
                {
                    artists.Add(artist.Id, artist);
                }
            }
            link.Artists = artists.Select(x => x.Value).ToList();
            _context.SaveChanges();
            
            return entry.Entity;
        }

        public Link DeleteLink(Guid linkId)
        {
            var link = _context.Links.Include(x => x.Domain).FirstOrDefault(x => x.Id == linkId);
            if (link == null)
            {
                throw new Exception($"Link {linkId} not found.");
            }
            link.IsActive = false;
            _context.SaveChanges();
            return link;
        }
    }
}
