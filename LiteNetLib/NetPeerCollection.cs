﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace LiteNetLib
{
    internal class IPEndPointComparer : IEqualityComparer<IPEndPoint>
    {
        public bool Equals(IPEndPoint x, IPEndPoint y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(IPEndPoint obj)
        {
            return obj.GetHashCode();
        }
    }

    internal sealed class NetPeerCollection
    {
        private readonly Dictionary<IPEndPoint, NetPeer> _peersDict;
        private readonly ReaderWriterLockSlim _lock;
        private NetPeer _headPeer;

        public int Count;

        public NetPeer HeadPeer
        {
            get { return _headPeer; }
        }

        public NetPeerCollection()
        {
            _peersDict = new Dictionary<IPEndPoint, NetPeer>(new IPEndPointComparer());
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        public bool TryGetValue(IPEndPoint endPoint, out NetPeer peer)
        {
            _lock.EnterReadLock();
            bool result = _peersDict.TryGetValue(endPoint, out peer);
            _lock.ExitReadLock();
            return result;
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            _headPeer = null;
            _peersDict.Clear();
            Count = 0;
            _lock.ExitWriteLock();
        }

        public void Add(IPEndPoint endPoint, NetPeer peer)
        {
            _lock.EnterWriteLock();
            peer.NextPeer = _headPeer;
            if (_headPeer != null)
            {
                _headPeer.PrevPeer = peer;
            }
            _headPeer = peer;
            _peersDict.Add(endPoint, peer);
            Count++;
            _lock.ExitWriteLock();
        }

        public void RemovePeers(List<NetPeer> peersList)
        {
            if (peersList.Count == 0)
                return;
            _lock.EnterWriteLock();
            for (int i = 0; i < peersList.Count; i++)
            {
                RemovePeerInternal(peersList[i]);
            }
            _lock.ExitWriteLock();
        }

        public void RemovePeer(NetPeer peer)
        {
            _lock.EnterWriteLock();
            RemovePeerInternal(peer);
            _lock.ExitWriteLock();
        }

        private void RemovePeerInternal(NetPeer peer)
        {
            if (!_peersDict.Remove(peer.EndPoint))
            {
                return;
            }
            if (peer == _headPeer)
            {
                _headPeer = peer.NextPeer;
            }
            if (peer.PrevPeer != null)
            {
                peer.PrevPeer.NextPeer = peer.NextPeer;
                peer.PrevPeer = null;
            }
            if (peer.NextPeer != null)
            {
                peer.NextPeer.PrevPeer = peer.PrevPeer;
                peer.NextPeer = null;
            }
            Count--;
        }
    }
}
