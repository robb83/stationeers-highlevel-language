# High level programming language for Stationeers

_Currently in an early stage, with limited functionality and untested features._\
_This project is primarily an experimental work for my own learning._

__This program compiles a custom C-like language into Stationeers MIPS instructions.__

## Examples:

<table>
<tr>
<td>Input</td>
<td>Output</td>
</tr>
</tr>
<tr>
<td colspan="2">
    Writes the external temperature to the attached memory and turns on all pumps on the network when the temperature drops below 410K (136.85°C).
</td>
</tr>
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
<tr>
<td colspan="2">
    Turns on the cooling device when the temperature exceeds 303K (29.85°C) and keep it on until the temperature drops below 295K (21.85°C).
</td>
</tr>
<tr>
<td valign="top">

```csharp
var sensor = Device(StructureGasSensor, d0);
var cooler = Device(StructureWallCooler, d1);

loop
{
	cooler.On = sensor.Temperature > (cooler.On ? 274 : 303);
	sleep(3);
}
```

</td>
<td valign="top">

```mips
loop_start001:
l r0 d0 Temperature
l r1 d1 On
select r1 r1 274 303
sgt r0 r0 r1
s d1 On r0
sleep 3
j loop_start001
loop_end001:
```

</td>
</tr>
<tr>
<td colspan="2">
    Toggle the GrowLight on and off every 10 minutes as long as the attached switch is turned on.
</td>
</tr>
<tr>
<td valign="top">

```csharp
var switch = Device(StructureLogicSwitch, d0);
var growLight = Device(StructureGrowLight, d1);
var light = 1;

loop
{
	var timer = 600;
	while (switch.Setting)
	{
		timer = timer - 0.5;

		if (timer <= 0)
		{
			growLight.On = light;
			light = xor(light, 1);
			break;
		}
	}
}
```

</td>
<td valign="top">

```mips
move r15 1
loop_start001:
move r14 600
while_start001:
l r0 d0 Setting
blt r0 1 while_end001
sub r14 r14 0.5
bgt r0 r14 0 if001
s d1 On r15
xor r15 r15 1
j while_end001
if001:
j while_start001
while_end001:
j loop_start001
loop_end001:
```

</td>
</tr>
<tr>
<td colspan="2">
    Turns on the generator when the battery level drops below 50% and charges it until it reaches 90% as long as the attached switch is turned on.
</td>
</tr>
<tr>
<td valign="top">

```csharp
var switch = Device(StructureLogicSwitch, d0);
var battery = Device(StructureBattery, d1);
var generator = Device(StructureSolidFuelGenerator, d2);

loop
{
	if (switch.Setting > 0)
	{
		generator.On = battery.Ratio < select(generator.On, 0.5, 0.9);
	} 
	else 
	{
		generator.On = 0;
	}
}

```

</td>
<td valign="top">

```mips
loop_start001:
l r0 d0 Setting
ble r0 r0 0 if_else001
l r0 d1 Ratio
l r3 d2 On
select r2 r3 0.5 0.9
slt r0 r0 r2
s d2 On r0
j if_end001
if_else001:
s d2 On 0
if_end001:
j loop_start001
loop_end001:
```

</td>
</tr>
</table>


## Language elements

```
# comment

# variable decleration
var temp = 0;

# assigment
temp = temp + 1;

# Loop statement
loop
{
    break;
    continue;
}

# Condition loop statement
while ( condition )
{
    break;
    continue;
}

# Condition statement
if ( condition )
{

}
elif ( condition )
{
    
} 
else 
{

}

# function call and constant usage
var temp = sin(45 * deg2rad);

# subrutine call
yield();
sleep(3);

# Configuration for attached device
var device1 = Device(StructureLogicMemory, d0);

# Configuration for GasSensors
var device2 = Device(StructureGasSensor, Minimum);

# Configuration for Named VolumePump or VolumePumps
var device3 = Device(StructureVolumePump, "Name", Minimum);

# Set and get logicType value
device1.Setting = device2.Temperature;
device3.On = device2.Temperature < 410;

# Access logicSlotType
var sorter = Device(StructureSorter, d2);
if (sorter[0].Occupied)
{

}

# Supported operation: + - * /
# Supported compare: < <= > >= == !=
# Supported logic: && ||
# Supported constant values: pi, nan, pinf, ninf, epsilon, deg2ead, rad2deg
# Supported built-in methods: sleep(r|num), yield(), hcf()
# Supported built-in functions: 
# abs(r|num), acos(r|num), asin(r|num), atan(r|num), atan2(r|num), ceil(r|num), cos(r|num), 
# exp(r|num), floor(r|num), log(r|num), rand(r|num), round(r|num), sin(r|num), sqrt(r|num), 
# tan(r|num), trunc(r|num), mod(r|num), max(r|num, r|num), min(r|num, r|num), xor(r|num, r|num), 
# nor(r|num, r|num), not(r|num, r|num), and(r|num, r|num), or(r|num, r|num), 
# sla(r|num, r|num), sll(r|num, r|num), sra(r|num, r|num), srl(r|num, r|num), select(r|num, r|num, r|num)
```

## Current Limitations:

- Optimised MIPS output, but not as handmade can be
- Cannot access device stack and reagent mix values
- Cannot access internal stack.
- Limited variable number (10 or less)
- Currently, error messages are not very user-friendly.
- Cannot define subroutines or functions.
- There is no validation for logicType or device type.