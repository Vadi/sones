/*
* sones GraphDB - Open Source Edition - http://www.sones.com
* Copyright (C) 2007-2010 sones GmbH
*
* This file is part of sones GraphDB Open Source Edition (OSE).
*
* sones GraphDB OSE is free software: you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License as published by
* the Free Software Foundation, version 3 of the License.
* 
* sones GraphDB OSE is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU Affero General Public License for more details.
*
* You should have received a copy of the GNU Affero General Public License
* along with sones GraphDB OSE. If not, see <http://www.gnu.org/licenses/>.
* 
*/

/* <id Name=�PandoraFS � NHDirectoryObject� />
 * <copyright file=�NHDirectoryObject.cs�
 *            company=�sones GmbH�>
 * Copyright (c) sones GmbH. All rights reserved.
 * </copyright>
 * <developer>Achim Friedland</developer>
 * <summary>This implements the data structure for handling (access-)
 * rights on file system objects.<summary>
 */

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace sones.GraphFS.InternalObjects
{

    
    [Flags]
    public enum NHIDirectoryObject : long
    {

        ObjectStream_Created,
        ObjectStream_Removed,

        DirectoryEntry_Created,
        DirectoryEntry_Changed,
        DirectoryEntry_Removed,

        InlineData_Created,
        InlineData_Changed,
        InlineData_Removed,

        Symlink_Created,
        Symlink_Changed,
        Symlink_Removed,

        IDirectoryObject_Removed

    }

}
