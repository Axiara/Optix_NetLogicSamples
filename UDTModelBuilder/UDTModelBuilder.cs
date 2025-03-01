#region Using directives
using System;
using System.Xml.Linq;

using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;

using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.SQLiteStore;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.NetLogic;
#endregion

/// <summary>
/// This NetLogic class creates a "Grid" object containing node pointers to each element in a PLC array,
/// allowing the Data Grid to display and update them without ownership conflicts.
/// </summary>
public class UDTModelBuilder : BaseNetLogic
{
    private IUAVariable gridModel;
    private IUAObject gridObject;
    private IUANode tagArray;

    public override void Start()
    {
        // Get the variables from the NetLogic object, and create a new object for the grid
        gridModel = LogicObject.GetVariable("GridModel");
        if (gridModel == null)
        {
            Log.Warning("Variable GridModel not found in NetLogic object.");
        }

        tagArray = GetTagArray("TagArray");
        gridObject = InformationModel.MakeObject("Grid");

        //Iterate through the array and add each element to the gridObject
        foreach (var elementNode in tagArray.GetOwnedNodes())
        {
            gridObject.Add(CreateNodePointer(elementNode));
        }

        //Set the gridModel's value to the Node ID of the gridObject
        gridModel.Value = gridObject.NodeId;
    }

    public override void Stop()
    {
        if (gridModel != null)
        {
            gridModel.Value = NodeId.Empty;
            gridModel = null;
        }

        if (tagArray != null)
        {
            tagArray = null;
        }

        if (gridObject != null)
        {
            gridObject.Delete();
            gridObject = null;
        }
    }

    private static IUAVariable CreateNodePointer(IUANode elementNode)
    {
        // Create the Node Pointer
        var pointerNode = InformationModel.MakeNodePointer($"gridNodePointer{elementNode.BrowseName}");

        // Store the NodeId of the PLC node in the pointer node's Value
        pointerNode.Value = elementNode.NodeId;

        return pointerNode;
    }

    private IUANode GetTagArray(string variableName)
    {
        // Grab the NetLogic variable from self
        IUAVariable tagArrayPtr = LogicObject.GetVariable(variableName);
        if (tagArrayPtr == null)
        {
            Log.Warning($"Variable {variableName} not found in NetLogic object.");
        }

        // Convert its value to a NodeId
        NodeId tagArrayNodeId = (NodeId)tagArrayPtr.Value;

        // Retrieve and return the node from the Information Model
        return InformationModel.Get(tagArrayNodeId);
    }
}
