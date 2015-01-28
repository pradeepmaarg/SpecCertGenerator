using System;
using Maarg.Fatpipe.LoggingService;

namespace Maarg.AllAboard
{
    /// <summary>
    /// This is the only class that clients will be aware of. Once clients get a DalManager, they invoke operations on it
    /// Note that GetDalManager returns an interface. That interface in turn consists of other interfaces or atomic data types
    /// Clients are not aware of any class inside that
    /// <param name="type">
    /// A string type based on which the factory decides to instantiate a specific DAL manager.
    /// Eg. value="DB", will return a Database based manager. Used in most scenarios
    ///     value="UnitTest", can return a dummy manager for unit testing and keeping components loosely coupled
    /// </param>
    /// <param name="parameters">
    /// Variable number of parameters specific to the corresponding type which represents physical storage
    /// Eg. for type="DB", use a single string parameter represending database connection string
    ///     for type="AzureStorage", use two parameters, a container name and Windows Azure 
    /// </param>
    /// </summary>
    public class DalManagerFactory
    {
        public static IDalManager GetDalManager(string type, params string[] parameters)
        {
            IDalManager dalManager = null;

            switch (type)
            {
                case "AzureStorage":
                    if (parameters == null || parameters.Length != 2)
                    {
                        string errorMessage = "There must be two parameters for the \"AzureStorage\" DAL type";
                        LoggerFactory.Logger.Error("DalManagerFactory.GetDalManager", EventId.DALGetManager, errorMessage);

                        throw new ArgumentException(errorMessage, "parameters");
                    }

                    string storageAccountConnectionString = parameters[0];
                    string containerName = parameters[1];

                    dalManager = new AzureDalManager(storageAccountConnectionString, containerName);
                    break;
                case "FileSystem":
                case "UnitTest":
                case "DB":
                    throw new NotImplementedException();
            }

            return dalManager;
        }
    }
}
