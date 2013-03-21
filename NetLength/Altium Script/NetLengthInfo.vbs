' NetLengthInfo.vbs
' By Theodore Beauchant
'
' This script is part of a set of tools that allows proper net length
' information to be reported on a per route basis (instead of total etch
' length).
'
' How to use:
'   - Modify the values of the variables in the Configuration block (below)
'     to fit your needs
'   - Make sure Excel is running with the excel spreadsheet open
'   - With the PCB to analyze being the active document, run the script
'     (DXP-> Run Script, or use a hotkey)
'
' Current limitations:
'   - The software checks for intersections between the extremities
'     of each line/arcs, so if two tracks intersects in their middle
'     it will not find the connection.
'   - All Pads are considered as a 3 mils diameter circle (their actual
'     specific shape is not taken in account). Make sure the tracks
'     reach the center of your pads (or close enough) for the process
'     to work.



Dim NetStart
Dim Source
Dim CountVias
Dim SoftPath

' --------------------------------------------------------------------------
'                          CONFIGURATION
' --------------------------------------------------------------------------

' NetStart: Comma separated list of regexs indicating the net to analyze
' ie. "^DDR_,^PCI_" will analyze all nets starting with DDR_ or PCI_
NetStart = "^SDDR_"

' SoftPath: Path to the analysis software
SoftPath = "C:\Altium scripts\NetLength.exe"

' CountVias: Set to true to take into account the via lengths
' The length will be calculated according to the Layer Stack dimensions
' Defaulted to 'False' since Specctra and Altium do not include
' them in their net length calculations
CountVias = False

' --------------------------------------------------------------------------


Sub ExportNets
    Dim CurrentPcb
    Dim Obj
    Dim ParentIterator
    Dim TrackData
    Dim FileName
    Dim fso
    Dim ReportFile
    Dim ReportDocument
    Dim Layer

    ' Checks if the current document is a PCB document
    If PcbServer Is Nothing Then Exit Sub
    Set CurrentPcb = PcbServer.GetCurrentPcbBoard
    If CurrentPcb Is Nothing Then Exit Sub

	FileName = ExtractFilePath(PCBServer.GetCurrentPCBBoard.FileName) & "NetLengthInfo.hyp"

	BeginHourGlass
	Call ResetParameters
	Call AddStringParameter("Format", "HYPERLYNX")
	Call AddStringParameter("Filename", FileName)
	Call RunProcess("PCB:Export")

    Dim command
    command = SoftPath & " """ & Filename & """ " & " """ & NetStart & """"
    If (CountVias = True) Then command = command & " -countvias"
    RunSystemCommand(command)

    EndHourGlass
End Sub
