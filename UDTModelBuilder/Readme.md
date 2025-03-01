## Converts a PLC UDT Array to a Model

-The Model property of Data Grids, List Boxes, and Comboboxes do not directly support a UDT array from the PLC.
-The lightweight Netlogic script in this example converts the UDT array into a object model that can be used in Optix.
-The Model generated directly references the PLC tag array with node pointers, they don't just copy the values. So updates to variables are fast and bidirectional.

To Use

-PLC tag for the UDT Array is linked to the TagArray Variable of the Netlogic Object image
![image](https://github.com/user-attachments/assets/3b79bd39-8a8e-4eea-81c1-65e7ff385224)

-Then, the Model of the DataGrid is linked to the GridModel variable of the Netlogic object image
![image](https://github.com/user-attachments/assets/c9d674c4-e74f-4c3c-9520-ba22788a3c0b)
