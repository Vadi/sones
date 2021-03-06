﻿#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sones.GraphDB.Structures.Enums;

using sones.Lib.ErrorHandling;

using sones.GraphDB.ObjectManagement;
using sones.GraphDB.TypeManagement;
using sones.GraphDB.Structures.EdgeTypes;
using sones.GraphDB.Exceptions;
using sones.GraphDB.Errors;
using sones.GraphDB.TypeManagement.BasicTypes;
using sones.GraphFS.DataStructures;
using sones.GraphDB.TypeManagement;

#endregion

namespace sones.GraphDB.Managers.Structures
{

    /// <summary>
    /// Abstract class to assign or update attributes
    /// </summary>
    public abstract class AAttributeAssignOrUpdate : AAttributeAssignOrUpdateOrRemove
    {

        #region Ctors
        
        public AAttributeAssignOrUpdate() { }

        public AAttributeAssignOrUpdate(IDChainDefinition myIDChainDefinition)
        {
            AttributeIDChain = myIDChainDefinition;
        }

        #endregion

        #region abstract GetValueForAttribute

        /// <summary>
        /// Return the value of an attribute
        /// </summary>
        /// <param name="myDBObject">The db object stream that contains the attribute</param>
        /// <param name="_dbContext">The db context</param>
        /// <param name="_graphDBType">The _graphDBType of the attribute</param>
        /// <returns>An exceptional of IObjects</returns>
        public abstract Exceptional<IObject> GetValueForAttribute(DBObjectStream myDBObject, DBContext myDBContext, GraphDBType myGraphDBType);

        #endregion

        #region Update

        /// <summary>
        /// Updates an existing attribute value
        /// </summary>
        /// <param name="_dbContext">The db context</param>
        /// <param name="myDBObjectStream">The objectstream to update</param>
        /// <param name="_graphDBType">The _graphDBType of the db objectstream</param>
        /// <returns>An excpetional with results</returns>
        public override Exceptional<Dictionary<string, Tuple<TypeAttribute, IObject>>> Update(DBContext myDBContext, DBObjectStream myDBObjectStream, GraphDBType myGraphDBType)
        {

            Dictionary<String, Tuple<TypeAttribute, IObject>> attrsForResult = new Dictionary<String, Tuple<TypeAttribute, IObject>>();

            if (base.AttributeIDChain.IsUndefinedAttribute)
            {

                #region Undefined attribute

                var applyResult = ApplyAssignUndefinedAttribute(myDBContext, myDBObjectStream, myGraphDBType);

                if (applyResult.Failed())
                {
                    return new Exceptional<Dictionary<string, Tuple<TypeAttribute, IObject>>>(applyResult);
                }

                if (applyResult.Value != null)
                {
                    //sthChanged = true;

                    #region Add to queryResult

                    attrsForResult.Add(applyResult.Value.Item1, new Tuple<TypeAttribute, IObject>(applyResult.Value.Item2, applyResult.Value.Item3));

                    #endregion
                
                }

                #endregion

            }

            else
            {

                #region Usual attribute

                var applyResult = ApplyAssignAttribute(this, myDBContext, myDBObjectStream, myGraphDBType);

                if (applyResult.Failed())
                {
                    return new Exceptional<Dictionary<string, Tuple<TypeAttribute, IObject>>>(applyResult);
                }

                if (applyResult.Value != null)
                {
                    //sthChanged = true;

                    #region Add to queryResult

                    attrsForResult.Add(applyResult.Value.Item1, new Tuple<TypeAttribute, IObject>(applyResult.Value.Item2, applyResult.Value.Item3));

                    #endregion
                
                }

                #endregion
            
            }

            return new Exceptional<Dictionary<string, Tuple<TypeAttribute, IObject>>>(attrsForResult);

        }


        #region override AAttributeAssignOrUpdateOrRemove.Update

        private Exceptional<Tuple<String, TypeAttribute, IObject>> ApplyAssignUndefinedAttribute(DBContext myDBContext, DBObjectStream myDBObjectStream, GraphDBType myGraphDBType)
        {
            Dictionary<String, Tuple<TypeAttribute, IObject>> attrsForResult = new Dictionary<String, Tuple<TypeAttribute, IObject>>();

            #region undefined attributes

            var newValue = GetValueForAttribute(myDBObjectStream, myDBContext, myGraphDBType);
            if (newValue.Failed())
            {
                return new Exceptional<Tuple<string, TypeAttribute, IObject>>(newValue);
            }

            if (myDBObjectStream.ContainsUndefinedAttribute(AttributeIDChain.UndefinedAttribute, myDBContext.DBObjectManager))
            {
                var removeResult =myDBObjectStream.RemoveUndefinedAttribute(AttributeIDChain.UndefinedAttribute, myDBContext.DBObjectManager);
                if (removeResult.Failed())
                {
                    return new Exceptional<Tuple<string, TypeAttribute, IObject>>(removeResult);
                }
            }

            //TODO: change this to a more handling thing than KeyValuePair
            var addExcept = myDBContext.DBObjectManager.AddUndefinedAttribute(AttributeIDChain.UndefinedAttribute, newValue.Value, myDBObjectStream);

            if (addExcept.Failed())
            {
                return new Exceptional<Tuple<String, TypeAttribute, IObject>>(addExcept);
            }

            //sthChanged = true;

            attrsForResult.Add(AttributeIDChain.UndefinedAttribute, new Tuple<TypeAttribute, IObject>(null, newValue.Value));

            #endregion

            return new Exceptional<Tuple<String, TypeAttribute, IObject>>(new Tuple<String, TypeAttribute, IObject>(AttributeIDChain.UndefinedAttribute, AttributeIDChain.LastAttribute, newValue.Value));

        }

        #endregion

        internal Exceptional<Tuple<String, TypeAttribute, IObject>> ApplyAssignAttribute(AAttributeAssignOrUpdate myAAttributeAssign, DBContext myDBContext, DBObjectStream myDBObject, GraphDBType myGraphDBType)
        {

            System.Diagnostics.Debug.Assert(myAAttributeAssign != null);

            //get value for assignement
            var aValue = myAAttributeAssign.GetValueForAttribute(myDBObject, myDBContext, myGraphDBType);
            if (aValue.Failed())
            {
                return new Exceptional<Tuple<String, TypeAttribute, IObject>>(aValue);
            }

            object oldValue = null;
            IObject newValue = aValue.Value;

            if (myDBObject.HasAttribute(myAAttributeAssign.AttributeIDChain.LastAttribute.UUID, myGraphDBType))
            {

                #region Update the value because it already exists

                oldValue = myDBObject.GetAttribute(myAAttributeAssign.AttributeIDChain.LastAttribute, myGraphDBType, myDBContext).Value;

                switch (myAAttributeAssign.AttributeIDChain.LastAttribute.KindOfType)
                {
                    case KindsOfType.SetOfReferences:
                        var typeOfCollection = ((AttributeAssignOrUpdateList)myAAttributeAssign).CollectionDefinition.CollectionType;

                        if (typeOfCollection == CollectionType.List)
                            return new Exceptional<Tuple<String, TypeAttribute, IObject>>(new Error_InvalidAssignOfSet(myAAttributeAssign.AttributeIDChain.LastAttribute.Name));

                        var removeRefExcept = RemoveBackwardEdgesOnReferences(myAAttributeAssign, (IReferenceEdge)oldValue, myDBObject, myDBContext);

                        if (!removeRefExcept.Success())
                            return new Exceptional<Tuple<String, TypeAttribute, IObject>>(removeRefExcept.IErrors.First());

                        newValue = (ASetOfReferencesEdgeType)newValue;
                        break;

                    case KindsOfType.SetOfNoneReferences:
                    case KindsOfType.ListOfNoneReferences:
                        newValue = (IBaseEdge)newValue;
                        break;

                    case KindsOfType.SingleNoneReference:
                        if (!(oldValue as ADBBaseObject).IsValidValue((newValue as ADBBaseObject).Value))
                        {
                            return new Exceptional<Tuple<string, TypeAttribute, IObject>>(new Error_DataTypeDoesNotMatch((oldValue as ADBBaseObject).ObjectName, (newValue as ADBBaseObject).ObjectName));
                        }
                        newValue = (oldValue as ADBBaseObject).Clone((newValue as ADBBaseObject).Value);
                        break;

                    case KindsOfType.SingleReference:
                        if (newValue is ASingleReferenceEdgeType)
                        {
                            removeRefExcept = RemoveBackwardEdgesOnReferences(myAAttributeAssign, (IReferenceEdge)oldValue, myDBObject, myDBContext);

                            if (!removeRefExcept.Success())
                                return new Exceptional<Tuple<String, TypeAttribute, IObject>>(removeRefExcept.IErrors.First());

                            ((ASingleReferenceEdgeType)oldValue).Merge((ASingleReferenceEdgeType)newValue);
                            newValue = (ASingleReferenceEdgeType)oldValue;
                        }
                        break;

                    case KindsOfType.SpecialAttribute: // Special attributes can't be updated currently

                        if ((newValue as DBString) != null && (newValue as DBString).CompareTo(oldValue) == 0)
                        {
                            return new Exceptional<Tuple<string, GraphDB.TypeManagement.TypeAttribute, GraphDB.TypeManagement.IObject>>(new Tuple<string, GraphDB.TypeManagement.TypeAttribute, GraphDB.TypeManagement.IObject>(myAAttributeAssign.AttributeIDChain.LastAttribute.Name, myAAttributeAssign.AttributeIDChain.LastAttribute, newValue as IObject));
                        }
                        else
                        {
                            return new Exceptional<Tuple<String, TypeAttribute, IObject>>(new Error_NotImplemented(new System.Diagnostics.StackTrace(true)));
                        }

                        break;

                    default:
                        return new Exceptional<Tuple<String, TypeAttribute, IObject>>(new Error_NotImplemented(new System.Diagnostics.StackTrace(true)));
                }

                #endregion

            }

            var alterExcept = myDBObject.AlterAttribute(myAAttributeAssign.AttributeIDChain.LastAttribute.UUID, newValue);

            if (alterExcept.Failed())
                return new Exceptional<Tuple<string, TypeAttribute, IObject>>(alterExcept);

            if (!alterExcept.Value)
            {
                myDBObject.AddAttribute(myAAttributeAssign.AttributeIDChain.LastAttribute.UUID, newValue);
            }

            #region add backward edges

            if (myAAttributeAssign.AttributeIDChain.LastAttribute.GetDBType(myDBContext.DBTypeManager).IsUserDefined)
            {
                Dictionary<AttributeUUID, IObject> userdefinedAttributes = new Dictionary<AttributeUUID, IObject>();
                userdefinedAttributes.Add(myAAttributeAssign.AttributeIDChain.LastAttribute.UUID, newValue);

                var omm = new ObjectManipulationManager(myDBContext, myGraphDBType);
                var setBackEdges = omm.SetBackwardEdges(userdefinedAttributes, myDBObject.ObjectUUID);

                if (setBackEdges.Failed())
                    return new Exceptional<Tuple<string, TypeAttribute, IObject>>(setBackEdges);
            }

            #endregion

            return new Exceptional<Tuple<String, TypeAttribute, IObject>>(new Tuple<String, TypeAttribute, IObject>(myAAttributeAssign.AttributeIDChain.LastAttribute.Name, myAAttributeAssign.AttributeIDChain.LastAttribute, newValue));
        }

        protected Exceptional<Boolean> RemoveBackwardEdgesOnReferences(AAttributeAssignOrUpdate myAAttributeAssign, IReferenceEdge myReference, DBObjectStream myDBObject, DBContext myDBContext)
        {
            foreach (var item in myReference.GetAllReferenceIDs())
            {
                var streamExcept = myDBContext.DBObjectCache.LoadDBObjectStream(myAAttributeAssign.AttributeIDChain.LastAttribute.GetDBType(myDBContext.DBTypeManager), (ObjectUUID)item);

                if (!streamExcept.Success())
                    return new Exceptional<Boolean>(streamExcept.IErrors.First());

                var removeExcept = myDBContext.DBObjectManager.RemoveBackwardEdge(streamExcept.Value, myAAttributeAssign.AttributeIDChain.LastAttribute.RelatedGraphDBTypeUUID, myAAttributeAssign.AttributeIDChain.LastAttribute.UUID, myDBObject.ObjectUUID);

                if (!removeExcept.Success())
                    return new Exceptional<Boolean>(removeExcept.IErrors.First());
            }

            return new Exceptional<Boolean>(true);
        }

        #endregion

    }

}
