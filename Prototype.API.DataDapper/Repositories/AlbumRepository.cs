﻿using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;
using Prototype.API.Domain;
using Prototype.API.Domain.ApiModels;
using Prototype.API.Domain.Entities;
using Prototype.API.Domain.Supervisors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prototype.API.DataDapper.Repositories
{
    public class AlbumRepository : IAlbumRepository
    {
        private readonly DbInfo _dbInfo;
        private readonly ILogger _logger;

        public AlbumRepository(DbInfo dbInfo, ILogger<AlbumRepository> logger)
        {
            _dbInfo = dbInfo;
            _logger = logger;
        }

        private IDbConnection Connection => new SqlConnection(_dbInfo.ConnectionStrings);

        public void Dispose()
        {
            Connection.Dispose();
        }

        private async Task<bool> AlbumExists(int id, CancellationToken ct = default) =>
            await Connection.ExecuteScalarAsync<bool>("select count(1) from Albums where AlbumId = @id", new { id });

        public async Task<IEnumerable<Album>> GetAllAsync(int offset, int limit, CancellationToken ct = default)
        {
            try
            {
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var sql = String.Format("Select * From Albums ORDER BY AlbumId OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", (offset - 1) * limit, limit);
                    var albums = await Connection.QueryAsync<Album>(sql);
                    return albums; //.ToList();
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Album> GetByIdAsync(int id, CancellationToken ct = default)
        {
            using (var cn = Connection)
            {
                cn.Open();
                var album = await cn.QueryFirstOrDefaultAsync<Album>("Select * From Albums WHERE AlbumId = @Id", new { id });
                return album;
            }
        }

        public async Task<List<Album>> GetByArtistIdAsync(int id, CancellationToken ct = default)
        {
            using (var cn = Connection)
            {
                cn.Open();
                var albums = await cn.QueryAsync<Album>("Select * From Albums WHERE ArtistId = @Id", new { id });
                return albums.ToList();
            }
        }

        public async Task<Album> AddAsync(Album newAlbum, CancellationToken ct = default)
        {
            using (var cn = Connection)
            {
                cn.Open();
                var albumId = await cn.InsertAsync(new Album { Title = newAlbum.Title, ArtistId = newAlbum.ArtistId }); //TODO handle exceptions

                newAlbum.AlbumId = albumId;
            }

            return newAlbum;
        }

        public async Task<bool> UpdateAsync(Album album, CancellationToken ct = default)
        {
            if (!await AlbumExists(album.AlbumId, ct))
                return false;

            try
            {
                using (var cn = Connection)
                {
                    cn.Open();
                    return await cn.UpdateAsync(album);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            try
            {
                using (var cn = Connection)
                {
                    cn.Open();
                    return await cn.DeleteAsync(new Album { AlbumId = id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
    }
}
