﻿using MongoDB.Driver;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public interface IMongoDbContext
    {
        IMongoCollection<T> GetCollection<T>() where T : DocumentEntity;
    }
}