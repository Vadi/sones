﻿/* <id name=”Libraries Datastructures – BPlusTree” />
 * <copyright file=”InnerNode.cs”
 *            company=”sones GmbH”>
 * Copyright (c) sones GmbH. All rights reserved.
 * </copyright>
 * <developer>Martin Junghanns</developer>
 */

#region usings

using System;
using System.Collections.Generic;
using System.Text;
using sones.Lib.DataStructures.Indices;

#endregion

namespace sones.Lib.DataStructures.BPlusTree
{
    class InnerNode<TKey, TValue> : Node<TKey, TValue>        
        where TKey : IComparable
    {

        #region private members

        /// <summary>
        /// The Chidren array holds the referenes to the nodes which
        /// have this node as it's parent.
        /// 
        /// children[first] points at a node which contains keys that are
        /// all smaller then the smallest key in here.
        /// childen[last] points at a node which contains keys that are all
        /// greater or equal to the greatest key in here.
        /// all pointers between point on nodes which contain keys that are
        /// greater or equal to the key which has the same index as the children.
        /// 
        /// example:
        /// 
        /// k1-3 are ordered key in that node
        /// p1-4 point on other nodes considering the information written above
        /// 
        ///  |k1|k2|k3|
        /// |p1|p2|p3|p4|
        ///  
        /// </summary>
        private Node<TKey, TValue>[] _Children;

        #endregion

        #region constructors

        public InnerNode(BPlusTree<TKey, TValue> myTree) : base(myTree)
        {
            _Children = new Node<TKey, TValue>[myTree.MaxChildrenPerNode + 1] ;
        }

        #endregion

        #region getter / setter

        /// <summary>
        /// Sets / Returns the children of that innernode
        /// </summary>
        public Node<TKey, TValue>[] Children
        {
            get { return _Children; }
            set { _Children = value; }
        }

        #endregion

        #region public methods

        public override bool Insert(TKey myKey, HashSet<TValue> myValue, ref SplitInfo<TKey, TValue> mySplitInfo, IndexSetStrategy myIndexSetStrategy)
        {
            #region data

            Int32 arrayIndex = _KeyCount - 1; //used to access the internal arrays
            bool returnVal;

            #endregion

            #region find slot

            //search for the next node to go
            //if the current key is smaller then the new one, decrement i
            while ((arrayIndex >= 0) && (myKey.CompareTo(this.Keys[arrayIndex]) == -1))
            {
                arrayIndex--;
            }

            //increment i to access the correct children slot
            arrayIndex++;

            //go down the tree
            returnVal = _Children[arrayIndex].Insert(myKey, myValue, ref mySplitInfo, myIndexSetStrategy);

            #endregion

            if (returnVal == SPLITTED)
            {
                /*
                 * the child node was splitted, so we have to add the key and set the new child reference
                 */

                for (Int32 i = this.KeyCount; i > arrayIndex; i--)
                {
                    _Keys[i] = _Keys[i - 1];
                    _Children[i + 1] = _Children[i];
                }

                //insert the propagated key and children
                _Keys[arrayIndex] = mySplitInfo.Key;
                _Children[arrayIndex + 1] = mySplitInfo.Node;

                //increment the key count
                _KeyCount++;

                /*
                 * now we have to check if the extra key was too much for our node
                 * if it's full, we have to split that inner node in two and propagate
                 * the splitkey to the parent node
                 */
                if (!IsFull())
                {
                    //nice
                    return NOT_SPLITTED;
                }
                else
                {
                    #region splitting

                    #region data

                    //create a new node which contains all keys and references greater then the middle key
                    InnerNode<TKey, TValue> newNode = new InnerNode<TKey, TValue>(_CorrespondingTree);
                    //the index where we split that node
                    Int32 splitIndex    = _KeyCount / 2;
                    //the key located at that index
                    var middleKey       = _Keys[splitIndex];

                    #endregion

                    #region siblings

                    //set the siblings (new node is right of splitnode)
                    newNode.LeftSibling = this;
                    newNode.RightSibling = this.RightSibling;
                    this.RightSibling = newNode;

                    #endregion

                    #region parent of new and children

                    //set the parent for the new node
                    newNode.Parent = this.Parent;
                    
                    #endregion

                    #region moving keys and children

                    //move middle key and all keys then the middle key into the new Node
                    Transfer(splitIndex + 1, 0, _KeyCount - splitIndex - 1, newNode);

                    //remove the last key-child pair in the old node
                    _Keys[_KeyCount - 1] = default(TKey);
                    _Children[_KeyCount] = null;
                    _KeyCount--;

                    #endregion

                    #region propagate splitinformation

                    /*
                     * the has to be inserted in the parent node
                     * and the new node is its right child because
                     * it contains all keys greater or equal then the
                     * inserted on
                     */
                    mySplitInfo.Key     = middleKey;
                    mySplitInfo.Node    = newNode;

                    #endregion

                    //done at that node
                    //tell the parent node, that there's some work
                    return SPLITTED;

                    #endregion
                }
            }
            else
            {
                //nothing to do
                return NOT_SPLITTED;
            }
        }

        /// <summary>
        /// Transfers entries of an inner node to another or the same node.
        /// </summary>
        /// <param name="mySourceStartIndex">Index to from in the source node</param>
        /// <param name="myDestinationStartIndex">Index to start from in the destination node</param>
        /// <param name="myLength">Number of entries to transfer</param>
        /// <param name="myDestinationNode">The destination node (can be the same as source node)</param>
        protected override void Transfer(int mySourceStartIndex, int myDestinationStartIndex, int myLength, Node<TKey, TValue> myDestinationNode)
        {
            //it's a leaf node, so cast it
            InnerNode<TKey, TValue> destinationNode = myDestinationNode as InnerNode<TKey, TValue>;

            /*
             * node looks like that
             * 
             *      k1 k2 k3 k4 k5
             *     p1 p2 p3 p4 p5 p6
             * 
             */
            int sourceIndex;
            int targetIndex;

            //if we transfer within the same node and we transfer from left to right we must use reverse order
            if (this.Equals(destinationNode) && mySourceStartIndex < myDestinationStartIndex)
            {
                int index;
                for (index = myDestinationStartIndex - 1; index >= 0; index--)
                {
                    //transfer keys
                    _Keys[index + myLength] = _Keys[index];
                    //transfer values
                    _Children[index + myLength + 1] = _Children[index + 1];
                    //set old key to default value
                    _Keys[index] = default(TKey);
                    //and value hashset null
                    _Children[index + 1] = null;
                }
                //move the left child
                _Children[index + myLength + 1] = _Children[index + 1];
                _Children[index + 1] = null;
            }
            else
            {
                //hack, used to check if something happened in the loop
                bool moved = false;

                //different nodes or same node but transfer from right to left
                for (sourceIndex = mySourceStartIndex, targetIndex = myDestinationStartIndex; sourceIndex < (mySourceStartIndex + myLength); sourceIndex++, targetIndex++)
                {
                    moved = true;
                    //transfer keys
                    myDestinationNode.Keys[targetIndex] = _Keys[sourceIndex];
                    //transfer values
                    destinationNode.Children[targetIndex] = _Children[sourceIndex];
                    //update parent pointer
                    destinationNode.Children[targetIndex].Parent = destinationNode;
                    //default value for key
                    _Keys[sourceIndex] = default(TKey);
                    //and child
                    _Children[sourceIndex] = null;
                    //count values
                    _KeyCount--;
                    myDestinationNode.KeyCount++;
                }
                if (moved)
                {
                    //move last child
                    destinationNode.Children[targetIndex] = _Children[sourceIndex];
                    //update childs parent
                    if (destinationNode.Children[targetIndex] != null) //is null in case of root removal
                    {
                        destinationNode.Children[targetIndex].Parent = destinationNode;
                    }
                    //and set source ref to null
                    _Children[sourceIndex] = null;
                }

                /*
                 * if the source index is less or equal then the destination index (left to right shift)
                 * we have to balance the node order.
                 * 
                 * Node looks like that now:
                 * 
                 *      - - - k4 k5
                 *     - - - p4 p5 p6
                 *     
                 * 
                 * we shift them to the beginning now
                 */
                if (mySourceStartIndex <= myDestinationStartIndex)
                {
                    //it's a right to left shift
                    Array.Copy(_Keys, (mySourceStartIndex + myLength), _Keys, 0, _KeyCount);
                    Array.Copy(_Children, (mySourceStartIndex + myLength + 1), _Children, 0, _KeyCount + 1);
                }
                /*
                 * now it looks like that
                 * 
                 *      k4 k5 - - -
                 *     p4 p5 p6 - - -
                 */
            }
        }

        /// <summary>
        /// Array.Copy works like that
        ///              
        ///  post: 
        ///  { 0, 1, 2, 3, 4, 5 }
        ///  toDeleteIndex = 2
        ///  
        ///  pre:
        ///  { 0, 1, 3, 4, 5, 5 }
        ///  dec(keyCount)
        /// </summary>
        /// <param name="myIndex"></param>
        /// <returns></returns>
        protected override bool RemoveInternal(TKey myKey)
        {
            int myIndex;
            if (!ContainsKey(myKey, out myIndex))
            {
                //nothing to remove in here
                return false;
            }
            else
            {
                //move keys greater then myIndex one slot left
                Array.Copy(_Keys, myIndex + 1, _Keys, myIndex, _KeyCount - myIndex - 1);

                //move children greater then myIndex one slot left
                Array.Copy(_Children, myIndex + 2, _Children, myIndex + 1, _KeyCount - myIndex - 1);

                //set the last value to it's default value
                _Keys[KeyCount - 1] = default(TKey);
                //and the last HashSet is dereferenced
                _Children[ChildrenCount - 1] = null;

                //finally increment node keycount
                _KeyCount--;

                //done
                return true;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("InnerNode");
            sb.AppendLine("KeyCount: " +_KeyCount);

            sb.AppendLine("Keys: ");
            for (int i = 0; i < _KeyCount; i++)
            {
                sb.Append(_Keys[i] + " ");
            }            
            sb.AppendLine();

            return sb.ToString();
        }


        #endregion
    }
}
