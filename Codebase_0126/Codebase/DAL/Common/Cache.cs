using System;
using System.Collections;
using System.Diagnostics;

namespace Maarg.AllAboard
{

	public class CacheManager
	{
		private static Hashtable mCacheMap = new Hashtable(10);
		private static object mLock = new object();

		/*
		 * Get a cache with the given name.
		 * If it does not exist, create one
		 */
		public static Cache GetCache(string cacheName, long timeoutSec, int size)
		{
			if (cacheName == null || cacheName == string.Empty)
			{
				throw new ArgumentNullException("cacheName");
			}

			if (size <= 0)
			{
				throw new ArgumentNullException("size");
			}

			Cache cache;
			lock (mLock)
			{
				cache = (Cache)mCacheMap[cacheName];
				if (cache == null)
				{
					cache = new Cache(cacheName, timeoutSec, size);
				}

				mCacheMap[cacheName] = cache;
			}

			return cache;
		}
	}

	/*
	 * This class implements an LRU based cache. Uses a helper linked list class
	 * to keep track of node usage
	 * Clients should use the following methods:
	 * AddObject(object userKey, object cacheObject)
	 * GetObject(object key)
	 */
	public class Cache
	{
		private Hashtable mMap = new Hashtable();
		private LinkedList mLru  = new LinkedList();
		private int mMaxSize = 0;
		private long mTimeoutSeconds = 0;
		private object mLock = new object();
		private string mName;
		

		public void AddObject(object userKey, object cacheObject)
		{
			if (userKey == null)
				throw new ArgumentNullException("userKey");

			if (cacheObject == null)
				throw new ArgumentNullException("cacheObject");

			lock (mLock)
			{
				RemoveNodeAndExpiredElements(userKey);
				ShrinkToSize(mMaxSize - 1);
				LruNode node = CreateNodeAndAppendMap(userKey, cacheObject);
			}
		}

		public object GetObject(object key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			object data = null;

			lock (mLock)
			{
				RemoveExpiredElements();
				LruNode node = (LruNode)mMap[key];

				if (node == null)
				{
					// cache miss
				}

				else if (node.IsExpired())
				{
					Debug.WriteLine("found expired object in cache for key " + key);
					Delete(node);
				}

				else
				{
					Debug.WriteLine("cache hit for key " + key);
					RevalueNode(node);
					data = node.Data;
				}

				return data;
			}
		}

		public Cache(string name, long timeoutSeconds, int maxSize)
		{
			mName = name;
			mTimeoutSeconds = timeoutSeconds;
			mMaxSize = maxSize;
		}

		public object RemoveNodeAndExpiredElements(object key)
		{
			RemoveExpiredElements();
			LruNode node = (LruNode)mMap[key];
			object oldVal = null;

			if (node != null)
			{
				Delete(node);
				oldVal = node.Data;
			}
			return oldVal;
		}


		private void RevalueNode(LruNode node)
		{
			mLru.MoveToFirst(node.NodePtr);
		}

		private void Delete(LruNode node)
		{
			mLru.Remove(node.NodePtr);
			mMap.Remove(node.Key);
		}

		private void RemoveLeastValuableNode()
		{
			LinkedListNode lln = mLru.PeekLast();
			LruNode node = (LruNode)lln.NodeValue;
			Delete(node);
		}

		private int ShrinkToSize(int desiredSize)
		{
			int deleted = 0;
			//Debug.WriteLine("ShrinkToSize called with des size = " + desiredSize + " count = " + mMap.Count); 

			if (desiredSize >= 0)
			{
				while (mMap.Count > desiredSize)
				{
					RemoveLeastValuableNode();
					deleted++;
				}
			}
			//Debug.WriteLine("# of deleted items = " + deleted);
			return deleted;
		}

		/*
		 * Method RemoveExpiredElements
		 */
		public void RemoveExpiredElements()
		{
			LinkedListNode llnode;
			LruNode node;
			int removeCount = 0;

			while ((llnode = mLru.PeekLast()) != null)
			{
				node = (LruNode)llnode.NodeValue;
				
				// getting mHeader node
				if (node.IsExpired())
				{
					Delete(node);
					Debug.WriteLine("Removing expired elemtnet with key " + node.Key + " from cache");
					removeCount++;
				}
				else
				{
					if (removeCount > 0)
					{
						Debug.WriteLine("RemoveExpiredElements removed " + removeCount + " nodes (" + mMap.Count + " left)");
					}

					break;
				}
			}
		}

		/**
		 * Method CreateNodeAndAppendMap
		 */
		private LruNode CreateNodeAndAppendMap(object userKey, object cacheObject)
		{
			LruNode node = new LruNode();
			node.Key = userKey;
			node.Data = cacheObject;
			node.NodePtr = mLru.AddFirst(node);
			if (mTimeoutSeconds < 0)
			{
				node.Timeout = -1;
			}

			else
			{
				long currTicks = System.DateTime.UtcNow.Ticks;
				node.Timeout =  currTicks + mTimeoutSeconds*10000000;
				//Debug.WriteLine("currTicks = " + currTicks + " timeoutSecs = " + mTimeoutSeconds 
				//	+ " expiry = " + node.Timeout);
			}

			mMap[userKey] = node;
			return node;
		}
	}

	public class LruNode
	{
		object mKey;
		object mData;
		LinkedListNode mLruNode;
		long mTimeout; 

		public object Key
		{
			get { return mKey; }
			set { mKey = value; }
		}

		public object Data
		{
			get { return mData; }
			set { mData = value; }
		}

		public LinkedListNode NodePtr
		{
			get { return mLruNode; }
			set { mLruNode = value; }
		}

		public long Timeout
		{
			get { return mTimeout; }
			set { mTimeout = value; }
		}

		public bool IsExpired()
		{
			bool expired = mTimeout > 0 && System.DateTime.UtcNow.Ticks > mTimeout;
			Debug.WriteLineIf(expired, "CacheEntry has expired");
			return expired;
			/*
			long ticks = System.DateTime.UtcNow.Ticks;
			Debug.WriteLine("currTicks = " + ticks + " expiry = " + mTimeout);
			bool expired = mTimeout > 0 && ticks > mTimeout;
			Debug.WriteLine("node expired = " + expired);
			return expired;
			*/
		}
	}

	/*
	 * This class represents a doubly linked list node
	 * It contains next and previous pointers and an object value
	 */
	public class LinkedListNode
	{
		//next pointer
		LinkedListNode mNext  = null;
		
		//prev pointer
		LinkedListNode mPrev  = null;
		
		//node value
		object mNodeValue = null;

		#region Properties
		public object NodeValue
		{
			get 
			{ 
				return mNodeValue; 
			}
			
			set 
			{ 
				mNodeValue = value; 
			}
		}

		
		public LinkedListNode Next
		{
			get 
			{
				return mNext;
			}

			set
			{
				mNext = value;
			}
		}

		
		public LinkedListNode Previous
		{
			get
			{
				return mPrev;
			}

			set
			{
				mPrev = value;
			}
		}
		#endregion
	}


	/*
	 * The linked list class
	 */
	public class LinkedList
	{
		//header node
		private LinkedListNode mHeader;
		
		//int size
		private int mSize;
		
		/*
		 * The constructor
		 */
		public LinkedList()
		{
			mHeader = new LinkedListNode();
			mHeader.NodeValue = mHeader;
			mHeader.Previous  = mHeader;
			mHeader.Next  = mHeader;
			mSize = 0;
		}

    
		/*
		 * This method adds to the beginning of the list
		 */
		public LinkedListNode AddFirst(object obj)
		{
			return AddBefore(mHeader.Next, obj);
		}

		/*
		 * This method adds to the end of the list
		 */
		public LinkedListNode AddLast(object obj)
		{
			return AddBefore(mHeader, obj);
		}

    
		/*
		 * Used to identify the last node of the linked list
		 */
		public LinkedListNode PeekLast()
		{
			return mHeader.Previous == mHeader ? null : mHeader.Previous;
		}

		
		public object Remove(LinkedListNode node)
		{
			if (node == null || node == mHeader)
			{
				return null;
			}

			node.Previous.Next = node.Next;
			node.Next.Previous = node.Previous;
			mSize--;
			return node.NodeValue;
		}
		
		public int Size
		{
			get { return mSize; }
		}

		
		public void MoveToFirst(LinkedListNode node)
		{
			Remove(node);
			AddBefore(mHeader.Next, node);
		}

		
		public void MoveToLast(LinkedListNode node)
		{
			Remove(node);
			AddBefore(mHeader, node);
		}

		
		private LinkedListNode AddBefore(LinkedListNode node, object obj)
		{
			LinkedListNode newNode = null;
			newNode = new LinkedListNode();
			newNode.NodeValue = obj;
			AddBefore(node, newNode);
			return newNode;
		}

		
		private void AddBefore(LinkedListNode nodeToAddBefore, 
			LinkedListNode newPreviousNode)
		{
			newPreviousNode.Previous = nodeToAddBefore.Previous;
			newPreviousNode.Next = nodeToAddBefore;
			newPreviousNode.Previous.Next = newPreviousNode;
			newPreviousNode.Next.Previous = newPreviousNode;
			mSize++;
		}    
	}
}