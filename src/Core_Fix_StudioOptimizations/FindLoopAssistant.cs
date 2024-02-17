using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IllusionFixes
{
    class FindLoopAssistant
    {
        class Frame
        {
            public Transform parent;
            public string childName;
            public int nextChildIndex;
            public int childCount;

            public Frame ( Transform transform )
            {
                parent = transform;
                nextChildIndex = 0;
                childCount = transform.childCount;
            }

            public Transform NextChild()
            {
                if (nextChildIndex < childCount)
                {
                    var child = parent.GetChild(nextChildIndex++);
                    childName = child.name;
                    return child;
                }

                return null;
            }
        }

        static Dictionary<string, List<string>> _nameToPathMap = new Dictionary<string, List<string>>();

        private HashSet<string> _skipNames = null;

        private Transform _root = null;
        private List<Frame> _findStack = new List<Frame>();
        private Dictionary<string, Transform> _childMap = new Dictionary<string, Transform>();

        //Variables to avoid GC
        private StringBuilder _pathBuilder = new StringBuilder();
        private List<string> _reversePaths = new List<string>();

        public FindLoopAssistant( IEnumerable<string> skipNames) 
        {
            _skipNames = new HashSet<string>(skipNames);
        }

        static Dictionary<string, int> counter = new Dictionary<string, int>();

        public Transform FindChild( Transform root, string findName )
        {
            if (_nameToPathMap.TryGetValue(findName, out var pathList))
            {
                for (int i = 0, n = pathList.Count; i < n; ++i)
                {
                    var child = root.Find(pathList[i]);
                    if (child != null)
                        return child;
                }
            }

            if ( _root == root )
            {
                if (_childMap.TryGetValue(findName, out var transform))
                {
                    RegisterPath(root, transform, findName);
                    return transform;
                }
            }
            else
            {
                _root = root;
                _childMap.Clear();
                _findStack.Clear();
                _findStack.Add(new Frame(root));
            }

            while ( _findStack.Count > 0 )
            {
                var last = _findStack[_findStack.Count - 1];

                var child = last.NextChild();

                if (child == null)
                {
                    _findStack.RemoveAt(_findStack.Count - 1);
                    continue;
                }

                string childName = last.childName;
                var childFrame = new Frame(child);

                if (!_skipNames.Contains(childName))
                    _findStack.Add(childFrame);

                if (string.CompareOrdinal(findName, childName) == 0)
                {
                    RegisterPath(findName);
                    return child;
                }

                // Register existing children to map
                if (!_childMap.ContainsKey(childName))
                    _childMap.Add(childName, child);
            }

            // Search for child is over. Not found.
            return null;
        }

        private void RegisterPath( string findName )
        {
            var stack = _findStack;
            var builder = _pathBuilder;
            builder.Length = 0;

            builder.Append(stack[0].childName);

            for ( int i = 1, n = stack.Count - 1; i < n; ++i )
            {
                builder.Append('/');
                builder.Append(stack[i].childName);
            }

            var path = builder.ToString();
            if (!_nameToPathMap.TryGetValue(findName, out var pathList))
                pathList = _nameToPathMap[findName] = new List<string>();

            pathList.Add(path);
        }

        private void RegisterPath( Transform root, Transform target, string findName )
        {
            var paths = _reversePaths;
            paths.Clear();

            while( root != target )
            {
                paths.Add(target.name);
                target = target.parent;
            }

            var builder = _pathBuilder;
            builder.Length = 0;
            builder.Append(paths[paths.Count - 1]);

            for ( int i = paths.Count - 2; i >= 0; --i )
            {
                builder.Append('/');
                builder.Append(paths[i]);
            }

            var path = builder.ToString();
            if (!_nameToPathMap.TryGetValue(findName, out var pathList))
                pathList = _nameToPathMap[findName] = new List<string>();

            pathList.Add(path);
        }
    }
}
