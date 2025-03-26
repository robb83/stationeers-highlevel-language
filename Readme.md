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

Loop
```csharp
loop
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
Supported logic: && ||
Supported built-in methods: sleep(r|num), yield()
Supported built-in functions: 
abs(r|num), acos(r|num), asin(r|num), atan(r|num), atan2(r|num), ceil(r|num), cos(r|num), 
exp(r|num), floor(r|num), log(r|num), rand(r|num), round(r|num), sin(r|num), sqrt(r|num), 
tan(r|num), trunc(r|num), mod(r|num), max(r|num, r|num), min(r|num, r|num), xor(r|num, r|num), 
nor(r|num, r|num), not(r|num, r|num), and(r|num, r|num), or(r|num, r|num), select(r|num, r|num, r|num)
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
lbn r0 -1252983604 1658757745 Temperature Minimum
s d0 Setting r0
lbn r0 -1252983604 1658757745 Temperature Minimum
slt r0 r0 410
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