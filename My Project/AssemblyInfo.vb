Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

' Le informazioni generali relative a un assembly sono controllate dal seguente 
' insieme di attributi. Per modificare le informazioni associate a un assembly
' occorre quindi modificare i valori di questi attributi.

' Rivedere i valori degli attributi dell'assembly

<Assembly: AssemblyTitle("SCFramework")> 
<Assembly: AssemblyDescription("")> 
<Assembly: AssemblyCompany("Carassai Samuele")> 
<Assembly: AssemblyProduct("SCFramework")>
<Assembly: AssemblyCopyright("Copyright © Samuele Carassai 2016")>
<Assembly: AssemblyTrademark("")> 

<Assembly: ComVisible(False)>

'Se il progetto viene esposto a COM, il GUID che segue verrà utilizzato per creare l'ID della libreria dei tipi
<Assembly: Guid("9bbb3cff-5334-47c1-957f-0aebb108d09b")> 

' Le informazioni sulla versione di un assembly sono costituite dai seguenti quattro valori:
'
'      Numero di versione principale
'      Numero di versione secondario 
'      Numero build
'      Revisione
'
' È possibile specificare tutti i valori oppure impostare valori predefiniti per i numeri relativi alla revisione e alla build 
' utilizzando l'asterisco (*) come descritto di seguito:
' <Assembly: AssemblyVersion("1.0.*")> 

<Assembly: AssemblyVersion("5.0.0.0")> 
<Assembly: AssemblyFileVersion("5.0.0.0")>

<Assembly: Web.UI.TagPrefix("SCFramework.WebControls", "SCFramework")>


' Resuorces

' Multi Languages Editor
<Assembly: Web.UI.WebResource("SCFramework.MultilanguageEditor.js", "text/javascript")>

