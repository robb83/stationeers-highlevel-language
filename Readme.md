# High level programming language for Stationeers

_Currently in an early stage, with limited functionality and untested features._\
_This project is primarily an experimental work for my own learning._

__This program compiles a custom C-like language into Stationeers MIPS instructions.__

## Language elements

Comment
```python
# comments
```

Variable declaration and assign
```csharp
var temp = 0;
temp = temp + 1;
```

Conditional statement
```csharp
if ( condition )
{

}
elif ( condition )
{
    
} 
else 
{

}
```

Conditional loop
```csharp
while ( condition )
{
    break;
    continue;
}
```

Device configuration for easier communication with external devices.

```csharp
var device1 = Device(StructureLogicMemory, d0);
var device2 = Device(StructureGasSensor, Minimum);
var device3 = Device(StructureVolumePump, "Name", Minimum);

device1.Setting = device2.Temperature;
device3.On = device2.Temperature < 410;

# var value = device.logicType;
# device.logicType = value;

```

```
Supported operation: + - * /
Supported conditions: < <= > >= == !=
```

## Example:

<table>
<tr>
<td valign="top">

```csharp
var memory = Device(StructureLogicMemory, d0);
var sensor = Device(StructureGasSensor, "External Gas Sensor", Minimum);
var pump = Device(StructureVolumePump, Minimum);

while (1)
{
	memory.Setting = sensor.Temperature;
	pump.On = sensor.Temperature < 410;
}
```

</td>
<td valign="top">

```mips
while_start001:
move r0 1
blt r0 1 while_end001
lbn r0 -1252983604 1658757745 Temperature Minimum
s d0 Setting r0
lbn r0 -1252983604 1658757745 Temperature Minimum
move r1 r0
move r0 410
slt r0 r1 r0
sb -321403609 On r0
j while_start001
while_end001:
```

</td>
</tr>
</table>

## Current Limitations:

- Unoptimized MIPS output
- Limited instruction set (no math and logic functions, sin, abs, not, ...)
- Cannot access device slots, stack and reagent mix values
- Limited variable number (10 or less)
- You cannot access internal stack.
- Currently, error messages are not user-friendly or missing.
- Cannot define subroutines or functions.
- There is no validation for logicType or device type.