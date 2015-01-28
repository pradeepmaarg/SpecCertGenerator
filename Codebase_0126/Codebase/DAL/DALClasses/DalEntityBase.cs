using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Maarg.Contracts;
using Maarg.Fatpipe.LoggingService;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.AllAboard.DALClasses
{
    /// <summary>
    /// Base class for entities that will be stored in blob storage.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public abstract class DalEntityBase<T>
        where T : class, IIdentifier, new()
    {
        /// <summary>
        /// Windows Azure Storage Account
        /// </summary>
        protected CloudStorageAccount storageAccount = null;

        /// <summary>
        /// Windows Azure Storage Blob Container name used to store Xml files 
        /// for all partners
        /// </summary>
        protected CloudBlobContainer container = null;

        /// <summary>
        /// Creates a new instance of the <see cref="DalEntityBase"/> type.
        /// </summary>
        /// <param name="storageAccount">The <see cref="CloudStorageAccount"/>.</param>
        /// <param name="container">The <see cref="CloudBlobContainer"/>.</param>
        public DalEntityBase(CloudStorageAccount storageAccount, CloudBlobContainer container)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException("storageAccount");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            this.storageAccount = storageAccount;
            this.container = container;
        }

        /// <summary>
        /// Gets the blob directory under which the entity will be stored.
        /// </summary>
        public abstract string BlobDirectoryName { get; }

        /// <summary>
        /// Returns the list of entities of type <see cref="T"/>.
        /// </summary>
        /// <returns>The list of entities of type <see cref="T"/>.</returns>
        public List<T> List()
        {
            string location = @"DalEntityBase.List";
            Stopwatch watch = Stopwatch.StartNew();

            List<T> result = new List<T>();

            // Use ConcurrentQueue to enable safe enqueueing from multiple threads.
            ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();
            try
            {
                CloudBlobClient client = this.storageAccount.CreateCloudBlobClient();
                CloudBlobDirectory directory = client.GetBlobDirectoryReference(string.Format(CultureInfo.InvariantCulture, "{0}/{1}", this.container.Name, this.BlobDirectoryName));
                BlobRequestOptions options = new BlobRequestOptions();
                options.UseFlatBlobListing = true;

                Parallel.ForEach(directory.ListBlobs(), currentBlob =>
                {
                    try
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            CloudBlob blob = container.GetBlobReference(currentBlob.Uri.ToString());
                            blob.DownloadToStream(memoryStream);
                            memoryStream.Seek(0, SeekOrigin.Begin);

                            BinaryFormatter binaryFormater = new BinaryFormatter();
                            T entity = binaryFormater.Deserialize(memoryStream) as T;
                            if (entity != null)
                            {
                                T finalEntity = this.GetExtended(entity);
                                result.Add(finalEntity);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        exceptions.Enqueue(exception);
                    }
                });

                if (exceptions.Any())
                {
                    throw new AggregateException(exceptions);
                }
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Warning(location, EventId.DALListItems
                    , "Error while getting the list of items in folder {0} in container {1}: {2}."
                    , this.BlobDirectoryName, this.container.Name, exception.ToString());
            }
            finally
            {
                watch.Stop();
                LoggerFactory.Logger.Debug(location, "List() for type {0} finished in {1} ms.", typeof(T), watch.ElapsedMilliseconds);
            }

            return result;
        }

        /// <summary>
        /// Gets an entity.
        /// </summary>
        /// <param name="identifier">The entity identifier.</param>
        /// <returns>The <see cref="T"/> entity.</returns>
        public T Get(string identifier)
        {
            string location = @"DalEntityBase.Get";
            Stopwatch watch = Stopwatch.StartNew();

            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("identifier");
            }

            T result = null;

            try
            {
                CloudBlobClient client = this.storageAccount.CreateCloudBlobClient();
                CloudBlob blob = client.GetBlobReference(string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", this.container.Name, this.BlobDirectoryName, identifier));
                using (MemoryStream stream = new MemoryStream())
                {
                    blob.DownloadToStream(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    BinaryFormatter formatter = new BinaryFormatter();
                    result = formatter.Deserialize(stream) as T;
                }

                if (result != null)
                {
                    result = this.GetExtended(result);
                }
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Warning(location, EventId.DALGetEntity
                    , "Error while getting entity {0} in folder {1} in container {2}: {3}."
                    , identifier, this.BlobDirectoryName, this.container.Name, exception.ToString());
            }
            finally
            {
                watch.Stop();
                LoggerFactory.Logger.Debug(location, "Get({0}) for type {1} finished in {2} ms.", identifier, typeof(T), watch.ElapsedMilliseconds);
            }

            return result;
        }

        /// <summary>
        /// Performs get operations specific for the <see cref="T"/> type.
        /// It is generally used to perform referential integrity operations.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be retrieved.</param>
        /// <returns>The full <see cref="T"/> entity.</returns>
        protected virtual T GetExtended(T entity)
        {
            return entity;
        }

        /// <summary>
        /// Saves the entity.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be saved.</param>
        public void Save(T entity)
        {
            string location = @"DalEntityBase.Save";
            Stopwatch watch = Stopwatch.StartNew();

            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, entity);
                    stream.Seek(0, SeekOrigin.Begin);

                    CloudBlobClient client = this.storageAccount.CreateCloudBlobClient();
                    CloudBlob blob = client.GetBlobReference(string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", this.container.Name, this.BlobDirectoryName, entity.Identifier));
                    blob.UploadFromStream(stream);
                }

                this.SaveExtended(entity);
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Error(location, EventId.DALSaveEntity
                    , "Error while saving entity {0} in folder {1} in container {2}: {3}."
                    , entity.Identifier, this.BlobDirectoryName, this.container.Name, exception.ToString());
                throw;
            }
            finally
            {
                watch.Stop();
                LoggerFactory.Logger.Debug(location, "Save({0}) for type {1} finished in {2} ms.", entity.Identifier, typeof(T), watch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Performs save operations specific for the <see cref="T"/> type.
        /// It is generally used to perform referential integrity operations.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be saved.</param>
        protected virtual void SaveExtended(T entity)
        {
        }

        /// <summary>
        /// Deletes the entity.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity.</param>
        public void Delete(T entity)
        {
            string location = @"DalEntityBase.Delete";
            Stopwatch watch = Stopwatch.StartNew();

            if (entity == null)
            {
                throw new ArgumentException("entity");
            }

            try
            {
                CloudBlobClient client = this.storageAccount.CreateCloudBlobClient();
                CloudBlob blob = client.GetBlobReference(string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", this.container.Name, this.BlobDirectoryName, entity.Identifier));
                blob.DeleteIfExists();

                this.DeleteExtended(entity);
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Error(location, EventId.DALDeleteEntity
                    , "Error while deleting entity {0} in folder {1} in container {2}: {3}."
                    , entity.Identifier, this.BlobDirectoryName, this.container.Name, exception.ToString());
                throw;
            }
            finally
            {
                watch.Stop();
                LoggerFactory.Logger.Debug(location, "Delete({0}) for type {1} finished in {2} ms.", entity.Identifier, typeof(T), watch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Performs delete operations specific for the <see cref="T"/> type.
        /// It is generally used to perform referential integrity operations.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be deleted.</param>
        protected virtual void DeleteExtended(T entity)
        {
        }
    }
}
