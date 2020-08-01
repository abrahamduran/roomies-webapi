using System;
namespace Roomies.WebAPI.Repositories
{
    public class RoomiesDatabaseSettings : IRoomiesDatabaseSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IRoomiesDatabaseSettings
    {
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}
