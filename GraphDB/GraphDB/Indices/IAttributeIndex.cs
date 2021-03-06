﻿/* <id name="GraphDB – IAttributeIndex" />
 * <copyright file="IAttributeIndex.cs"
 *            company="sones GmbH">
 * Copyright (c) sones GmbH. All rights reserved.
 * </copyright>
 * <developer>Henning Rauch</developer>
 * <summary>Interface for AttributeIndex and UUIDIdx</summary>
 */

#region Usings

using System;
using System.Collections.Generic;
using sones.GraphDB.ObjectManagement;
using sones.GraphDB.TypeManagement;
using sones.GraphFS.DataStructures;
using sones.Lib.DataStructures.Indices;
using sones.Lib.ErrorHandling;
using sones.Lib.NewFastSerializer;
using sones.Lib.Serializer;

#endregion

namespace sones.GraphDB.Indices
{
    /// <summary>
    /// Interface for AttributeIndex and UUIDIdx
    /// </summary>
    /// This could be deleted
    public interface IAttributeIndex : IFastSerializationTypeSurrogate, IFastSerialize, IVersionedIndexInterface<IndexKey, ObjectUUID>
    {

        #region Update

        /// <summary>
        /// This method updates the idx corresponding to an DBObject
        /// </summary>
        /// <param name="myDBObject">The DBObject that should be updated</param>
        /// <param name="myTypeOfDBObject">The type of the DBObject</param>
        /// <param name="myToken">The SessionInfos</param>
        Exceptional Update(DBObjectStream myDBObject, GraphDBType myTypeOfDBObject, DBContext dbContext);

        #endregion

        #region Insert

        /// <summary>
        /// This method inserts the given DBObject into the index
        /// </summary>
        /// <param name="myDBObject">The DBObject that should be inserted</param>
        /// <param name="myTypeOfDBobject">The type of the DBObject</param>
        /// <param name="myToken">The SessionInfos</param>
        Exceptional Insert(DBObjectStream myDBObject, GraphDBType myTypeOfDBobject, DBContext dbContext);

        /// <summary>
        /// This method inserts the given DBObject into the index
        /// </summary>
        /// <param name="myDBObject">The DBObject that should be inserted</param>
        /// <param name="myIndexSetStrategy">The index merge strategy</param>
        /// <param name="myTypeOfDBObject">The type of the DBObject</param>
        /// <param name="myToken">The SessionInfos</param>
        Exceptional Insert(DBObjectStream myDBObject, IndexSetStrategy myIndexSetStrategy, GraphDBType myTypeOfDBObject, DBContext dbContext);
        
        #endregion

        #region Contains
       
        /// <summary>
        /// This method checks if the current attribute index contains a DBObject
        /// </summary>
        /// <param name="myDBObject">The DBObject that should be checked</param>
        /// <param name="myTypeOfDBObject">The Type of the DBObject</param>
        /// <param name="myToken">The SessionInfos</param>
        /// <returns>A Trinary</returns>
        Boolean Contains(DBObjectStream myDBObject, GraphDBType myTypeOfDBObject, DBContext dbContext);

        Boolean Contains(IndexKey myIndeyKey, GraphDBType myTypeOfDBObject, DBContext dbContext);

        #endregion

        #region Remove

        /// <summary>
        /// This method removes a given DBObject from the index
        /// </summary>
        /// <param name="myDBObject">The DBObject that should be removed</param>
        /// <param name="myTypeOfDBObjects">The type of the DBObject</param>
        /// <param name="myToken">The SessionInfos</param>
        Exceptional Remove(DBObjectStream myDBObject, GraphDBType myTypeOfDBObjects, DBContext dbContext);

        #endregion

        #region Clear

        /// <summary>
        /// Clears the index
        /// </summary>
        /// <param name="indexManager">The database index manager</param>
        Exceptional ClearAndRemoveFromDisc(DBIndexManager indexManager);

        #endregion

        #region GetKeys

        /// <summary>
        /// Returns all IndexKeys of an AttributeIndex
        /// </summary>
        /// <returns>Enumerable of IndexKey</returns>
        IEnumerable<IndexKey> GetKeys(GraphDBType myTypeOfDBObject, DBContext dbContext);

        #endregion

        #region GetValues

        /// <summary>
        /// Returns all ObjectsUUIDs corresponding to an IndexKey
        /// </summary>
        /// <param name="myIndeyKey">The interesting IndexKey</param>
        /// <returns></returns>
        IEnumerable<ObjectUUID> GetValues(IndexKey myIndeyKey, GraphDBType myTypeOfDBObject, DBContext dbContext);

        #endregion

        #region GetAllValues

        /// <summary>
        /// Returns all values of an AttributeIndex
        /// </summary>
        /// <returns>Enumerable of all index values</returns>
        IEnumerable<IEnumerable<ObjectUUID>> GetAllValues(GraphDBType myTypeOfDBObject, DBContext dbContext);

        #endregion

        #region GetKeyValue

        IEnumerable<KeyValuePair<IndexKey, HashSet<ObjectUUID>>> GetKeyValues(GraphDBType myTypeOfDBObject, DBContext dbContext);

        #endregion

        #region GetValueCount

        UInt64 GetValueCount();

        #endregion

        #region GetKeyCount

        UInt64 GetKeyCount();

        #endregion

        #region InRange

        IEnumerable<ObjectUUID> InRange(IndexKey fromIndexKey, IndexKey toIndexKey, bool myOrEqualFromKey, bool myOrEqualToKey, GraphDBType myTypeOfDBObject, DBContext dbContext);

        #endregion

    }
}
