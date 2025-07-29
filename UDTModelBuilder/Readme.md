# FactoryTalk Optix – PLC UDT Array → Data Model (NetLogic)

Convert a **Logix/PLC UDT array** into a **DataGrid/ListBox/ComboBox**‑ready **model** in **FactoryTalk Optix**.
This NetLogic builds a lightweight “Grid” object containing **NodePointers** to each element of the PLC array so Optix UI controls can bind to it as a **Model**. Because the pointers reference the original nodes (no copying), changes in the UI read/write the original PLC tags (subject to permissions). ([Rockwell Automation][1])

---

## Why you need this

Optix UI controls (e.g., **Data grid**) expect the **Model** to be a node containing child items (objects/variables). A raw PLC array cannot be bound directly as such; wrapping the array as a folder/object with children is required so the grid can iterate items and display fields/columns. This NetLogic does that wrapping for you at runtime. ([Rockwell Automation][2], [Rockwell Automation][3])

---

## What it does

* Creates a **Grid** object at startup.
* Iterates the PLC UDT array node and, for each element, creates a **NodePointer** child targeting that element.
* Sets your `GridModel` variable to the NodeId of the new **Grid** object so UI controls can bind their **Model** to it.
* On stop, cleans up the temporary object and clears the model reference.
  *(This avoids “ownership”/binding conflicts by never cloning data—only pointing at the originals.)* ([Rockwell Automation][1])

---

## Code (NetLogic)

```csharp
public class UDTModelBuilder : BaseNetLogic
{
    private IUAVariable gridModel;
    private IUAObject gridObject;
    private IUANode tagArray;

    public override void Start()
    {
        gridModel = LogicObject.GetVariable("GridModel");
        tagArray  = GetTagArray("TagArray");

        gridObject = InformationModel.MakeObject("Grid");
        foreach (var elementNode in tagArray.GetOwnedNodes())
            gridObject.Add(CreateNodePointer(elementNode));

        gridModel.Value = gridObject.NodeId;
    }

    public override void Stop()
    {
        if (gridModel != null) gridModel.Value = NodeId.Empty;
        gridObject?.Delete();
    }

    private static IUAVariable CreateNodePointer(IUANode elementNode)
    {
        var ptr = InformationModel.MakeNodePointer($"gridNodePointer{elementNode.BrowseName}");
        ptr.Value = elementNode.NodeId;       // point at PLC array element
        return ptr;
    }

    private IUANode GetTagArray(string variableName)
    {
        var ptr = LogicObject.GetVariable(variableName);
        var id  = (NodeId)ptr.Value;
        return InformationModel.Get(id);
    }
}
```

---

## Project setup

1. **Add the NetLogic**

   * Add a **Runtime NetLogic** to your project and paste the class above. ([Rockwell Automation][4], [Rockwell Automation][5])

2. **Create two variables on the NetLogic node**

   * `TagArray` (`NodePointer`): points to your PLC UDT **array** root (e.g., `Controller/Program/Tags/MyUDT_Array`).
   * `GridModel` (`NodeId` or generic): this is **output**; the NetLogic sets it to the generated Grid object’s NodeId.
     *(NodePointer creation & linking guidance is in Optix help.)* ([Rockwell Automation][1])

3. **Link the PLC array**

   * In **Properties** of `TagArray`, set a **dynamic link** to the PLC UDT array node coming from your driver (e.g., RA EtherNet/IP). ([Rockwell Automation][1])

4. **Bind UI controls**

   * **Data grid** → set **Model** = `GridModel`.
   * Add columns whose cell bindings reference fields under `{Item}` (the pointed UDT element), e.g.:

     * Column 1 (Text): `{Item}/Member1`
     * Column 2 (Text): `{Item}/Member2`
   * Optix determines the **ItemKind** from the children of the model; set explicitly if needed. ([Rockwell Automation][2], [Rockwell Automation][3])

5. **Run**

   * At runtime the grid will show one row per array element; editing supported fields updates the original PLC nodes.

---

## Example: Binding a column

* **DataGridColumn → Text**: `Dynamic Link` → `{Item}/StatusText`
* **DataGridColumn → IsEnabled**: `Dynamic Link` → `{Item}/Enable` *(bool)*
* **DataGrid → Items count** is implicit: one child per pointer under the model. ([Rockwell Automation][3])

---

## Notes & Tips

* **Performance**: This is pointer‑based, so it’s lightweight—no periodic copy/sync code is needed.
* **Read/Write**: Whether edits write back depends on the referenced node’s permissions and driver configuration. NodePointer simply targets the original node. ([Rockwell Automation][1])
* **ItemKind**: If the grid doesn’t auto‑detect the type of `{Item}`, set **ItemKind** explicitly to the UDT/object type you’re displaying. ([Rockwell Automation][2])
* **Dynamic columns**: If your UDT changes shape or you want to generate columns dynamically, see Optix samples for dynamic DataGrid creation. ([GitHub][6])

---

## Troubleshooting

* **Blank grid**

  * Verify `TagArray` points directly to the **array** node (not its parent).
  * Ensure the PLC driver exposes array **elements as children** under the array node (so `GetOwnedNodes()` returns items). ([Rockwell Automation][3])
* **Wrong/missing fields in rows**

  * Confirm your column bindings (e.g., `{Item}/MemberName`) match the UDT member names.
  * Check **ItemKind** on the DataGrid. ([Rockwell Automation][2])
* **Edits don’t stick**

  * Check write permissions / data types on the target PLC tags and comms driver status.

---

## Related references

* **Data grid** object & Model concept. ([Rockwell Automation][3])
* **Configure a Data grid** – how the model maps to child nodes & `ItemKind`. ([Rockwell Automation][2])
* **NodePointer** – referencing other nodes via dynamic link. ([Rockwell Automation][1])
* **NetLogic** – creating & using runtime NetLogic in Optix. ([Rockwell Automation][4], [DMC, Inc.][7])
* **Sample** – dynamic DataGrid creation. ([GitHub][6])

---

## License

See repo root

---

## Acknowledgments

Thanks to the FactoryTalk Optix documentation & community examples that clarify **Model** and **NodePointer** usage in UI binding scenarios. ([Rockwell Automation][2], [Rockwell Automation][3], [Rockwell Automation][1])


[1]: https://www.rockwellautomation.com/en-hu/docs/factorytalk-optix/1-10/contents-ditamap/using-the-software/objects-and-variables/variables/create-a-node-pointer.html?utm_source=chatgpt.com "Create a node pointer - Rockwell Automation"
[2]: https://www.rockwellautomation.com/en-gb/docs/factorytalk-optix/1-03/contents-ditamap/developing-solutions/object-examples/data-grid-example/configure-a-data-grid.html?utm_source=chatgpt.com "Configure a Data grid - Rockwell Automation"
[3]: https://www.rockwellautomation.com/en-fi/docs/factorytalk-optix/1-00/contents-ditamap/developing-solutions/object-and-variable-references/ftoptix-ui/objecttypes/datagrid.html?utm_source=chatgpt.com "Data grid - Rockwell Automation"
[4]: https://www.rockwellautomation.com/en-id/docs/factorytalk-optix/1-5-7/contents-ditamap/tutorials/opc-ua-tutorial/develop-a-solution-for-importing-translations/client-project/create-a-netlogic-that-fetches-panels.html?utm_source=chatgpt.com "Create a NetLogic that fetches panels - Rockwell Automation"
[5]: https://www.rockwellautomation.com/en-za/docs/factorytalk-optix/1-00/contents-ditamap/developing-solutions/application-examples/netlogic-tutorial/develop-a-solution-for-importing-objects/create-the-alarm-importer-netlogic.html?utm_source=chatgpt.com "Create the AlarmImporter NetLogic - Rockwell Automation"
[6]: https://github.com/FactoryTalk-Optix/Optix_Sample_DynamicDataGridCretion?utm_source=chatgpt.com "Dynamic creation of DataGrid columns - GitHub"
[7]: https://www.dmcinfo.com/blog/16280/factorytalk-optix-series-3-netlogic-overview-and-examples/?utm_source=chatgpt.com "FactoryTalk Optix Series 3 - NetLogic Overview and Examples"



-PLC tag for the UDT Array is linked to the TagArray Variable of the Netlogic Object image
![image](https://github.com/user-attachments/assets/3b79bd39-8a8e-4eea-81c1-65e7ff385224)

-The Model of the DataGrid is linked to the GridModel variable of the Netlogic object image
![image](https://github.com/user-attachments/assets/c9d674c4-e74f-4c3c-9520-ba22788a3c0b)
