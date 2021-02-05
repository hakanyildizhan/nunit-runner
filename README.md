# NUnit Runner Readme

This is a Windows executable (.exe) application that offers parallel run capability for NUnit Tests. 

## How To Use

* NUnitRunner.exe will require these 4 parameters in this order:
  * Full path of the NUnit Test Assembly
  * Path of the NUnit console test executor
  * XML configuration file
  * Output directory
	
* An example configuration XML file can be found under *ExampleConfigurationFiles* folder.

* Example call:
*NUnitRunner.exe "C:\MyFolder\MyNUnitTestAssembly.dll" "C:\NUnitConsole\nunit-console.exe" "C:\XmlConfigPath\MyConfiguration.xml" "D:\MyOutputFolder"*