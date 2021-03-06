﻿using System;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class MeetingRepository : EntityRepository<MeetingInfoValues>, IMeetingRepository
    {
        public MeetingRepository(MeetingInfoValuesCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public MeetingInfo GetMeetingInfo(string uniqueId)
        {
            return Collection.FindOne(Query<MeetingInfoValues>.Where(i => i.Id == uniqueId));
        }

        public MeetingInfo[] GetMeetingInfo(string[] uniqueIds)
        {
            return Collection.Find(Query<MeetingInfoValues>.In(i => i.Id, uniqueIds)).Cast<MeetingInfo>().ToArray();
        }

        public void StartMeeting(string uniqueId)
        {
            var update = Update<MeetingInfoValues>
                .Set(i => i.IsStarted, true)
                .Set(i => i.LastModified, DateTime.Now);

            var result = Collection.Update(Query<MeetingInfoValues>.Where(i => i.Id == uniqueId), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }

        public void CancelMeeting(string uniqueId)
        {
            var update = Update<MeetingInfoValues>
                .Set(i => i.IsCancelled, true)
                .Set(i => i.LastModified, DateTime.Now);

            var result = Collection.Update(Query<MeetingInfoValues>.Where(i => i.Id == uniqueId), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }

        public void EndMeeting(string uniqueId)
        {
            var update = Update<MeetingInfoValues>
                .Set(i => i.IsEndedEarly, true)
                .Set(i => i.LastModified, DateTime.Now);

            var result = Collection.Update(Query<MeetingInfoValues>.Where(i => i.Id == uniqueId), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }
    }
}