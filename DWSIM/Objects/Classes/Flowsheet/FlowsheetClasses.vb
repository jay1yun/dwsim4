﻿'    Main Form Auxiliary Classes
'    Copyright 2008 Daniel Wagner O. de Medeiros
'
'    This file is part of DWSIM.
'
'    DWSIM is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    DWSIM is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with DWSIM.  If not, see <http://www.gnu.org/licenses/>.

Imports DWSIM.DrawingTools.GraphicObjects
Imports DWSIM.Thermodynamics.BaseClasses
Imports System.Linq

Namespace DWSIM.Flowsheet

    Public Enum MessageType
        Information
        Warning
        GeneralError
        Tip
    End Enum

    <System.Serializable()> Public Class ObjectCollection

        Public GraphicObjectCollection As Dictionary(Of String, GraphicObject)

        Public FlowsheetObjectCollection As Dictionary(Of String, SharedClasses.UnitOperations.BaseClass)

        Public OPT_SensAnalysisCollection As List(Of DWSIM.Optimization.SensitivityAnalysisCase)

        Public OPT_OptimizationCollection As List(Of DWSIM.Optimization.OptimizationCase)

        Sub New()

            'Creates all the graphic collections.

            GraphicObjectCollection = New Dictionary(Of String, GraphicObject)

            FlowsheetObjectCollection = New Dictionary(Of String, SharedClasses.UnitOperations.BaseClass)

            OPT_SensAnalysisCollection = New List(Of DWSIM.Optimization.SensitivityAnalysisCase)

            OPT_OptimizationCollection = New List(Of DWSIM.Optimization.OptimizationCase)

        End Sub

    End Class

    <System.Serializable()> Public Class FlowsheetVariables

        Implements Interfaces.ICustomXMLSerialization

        Implements Interfaces.IFlowsheetOptions

        Public Property FlashAlgorithms As New List(Of Interfaces.IFlashAlgorithm) Implements Interfaces.IFlowsheetOptions.FlashAlgorithms

        Public AvailableUnitSystems As New Dictionary(Of String, SystemsOfUnits.Units)

        Public PropertyPackages As Dictionary(Of String, PropertyPackages.PropertyPackage)

        Public ReadOnly Property SelectedPropertyPackage() As PropertyPackages.PropertyPackage
            Get
                For Each pp2 As PropertyPackages.PropertyPackage In PropertyPackages.Values
                    Return pp2
                    Exit For
                Next
                Return Nothing
            End Get
        End Property

        Public SelectedComponents As Dictionary(Of String, Interfaces.ICompoundConstantProperties)

        Public NotSelectedComponents As Dictionary(Of String, Interfaces.ICompoundConstantProperties)

        Public Databases As Dictionary(Of String, String())

        Public Reactions As Dictionary(Of String, Interfaces.IReaction)

        Public ReactionSets As Dictionary(Of String, Interfaces.IReactionSet)

        Public Property SimulationMode As String = ""

        Public PetroleumAssays As Dictionary(Of String, DWSIM.Utilities.PetroleumCharacterization.Assay.Assay)

        Public SelectedUnitSystem As SystemsOfUnits.Units

        Sub New()

            SelectedComponents = New Dictionary(Of String, Interfaces.ICompoundConstantProperties)
            NotSelectedComponents = New Dictionary(Of String, Interfaces.ICompoundConstantProperties)
            SelectedUnitSystem = New SystemsOfUnits.SI()
            Reactions = New Dictionary(Of String, Interfaces.IReaction)
            ReactionSets = New Dictionary(Of String, Interfaces.IReactionSet)
            Databases = New Dictionary(Of String, String())
            PropertyPackages = New Dictionary(Of String, PropertyPackages.PropertyPackage)
            PetroleumAssays = New Dictionary(Of String, DWSIM.Utilities.PetroleumCharacterization.Assay.Assay)

            With ReactionSets
                .Add("DefaultSet", New ReactionSet("DefaultSet", DWSIM.App.GetLocalString("Rxn_DefaultSetName"), DWSIM.App.GetLocalString("Rxn_DefaultSetDesc")))
            End With

        End Sub

        Public Function LoadData(data As System.Collections.Generic.List(Of System.Xml.Linq.XElement)) As Boolean Implements Interfaces.ICustomXMLSerialization.LoadData

            Dim el As XElement = (From xel As XElement In data Select xel Where xel.Name = "VisibleProperties").SingleOrDefault

            If Not el Is Nothing Then

                VisibleProperties.Clear()

                For Each xel2 As XElement In el.Elements
                    VisibleProperties.Add(xel2.@Value, New List(Of String))
                    For Each xel3 In xel2.Elements
                        VisibleProperties(xel2.@Value).Add(xel3.@Value)
                    Next
                Next

            End If

            FlashAlgorithms.Clear()

            el = (From xel As XElement In data Select xel Where xel.Name = "FlashAlgorithms").SingleOrDefault

            If Not el Is Nothing Then
                For Each xel As XElement In el.Elements
                    Dim obj As PropertyPackages.Auxiliary.FlashAlgorithms.FlashAlgorithm = New PropertyPackages.RaoultPropertyPackage().ReturnInstance(xel.Element("Type").Value)
                    obj.LoadData(xel.Elements.ToList)
                    FlashAlgorithms.Add(obj)
                Next
            Else
                FlashAlgorithms.Add(New Thermodynamics.PropertyPackages.Auxiliary.FlashAlgorithms.NestedLoops() With {.Tag = .Name})
            End If

            Return XMLSerializer.XMLSerializer.Deserialize(Me, data)

        End Function

        Public Function SaveData() As System.Collections.Generic.List(Of System.Xml.Linq.XElement) Implements Interfaces.ICustomXMLSerialization.SaveData

            Dim elements As System.Collections.Generic.List(Of System.Xml.Linq.XElement) = XMLSerializer.XMLSerializer.Serialize(Me)

            elements.Add(New XElement("VisibleProperties"))

            For Each item In VisibleProperties
                Dim xel2 = New XElement("ObjectType", New XAttribute("Value", item.Key))
                elements(elements.Count - 1).Add(xel2)
                For Each item2 In item.Value
                    xel2.Add(New XElement("PropertyID", New XAttribute("Value", item2)))
                Next
            Next

            elements.Add(New XElement("FlashAlgorithms"))

            For Each fa In FlashAlgorithms
                elements(elements.Count - 1).Add(New XElement("FlashAlgorithm", {New XElement("ID", fa.Tag), DirectCast(fa, Interfaces.ICustomXMLSerialization).SaveData().ToArray()}))
            Next

            Return elements

        End Function

        Public Property BackupFileName As String = "" Implements Interfaces.IFlowsheetOptions.BackupFileName

        Public Property FilePath As String = "" Implements Interfaces.IFlowsheetOptions.FilePath

        Public Property FlowsheetQuickConnect As Boolean = False Implements Interfaces.IFlowsheetOptions.FlowsheetQuickConnect

        Public Property FlowsheetShowCalculationQueue As Boolean = False Implements Interfaces.IFlowsheetOptions.FlowsheetShowCalculationQueue

        Public Property FlowsheetShowConsoleWindow As Boolean = False Implements Interfaces.IFlowsheetOptions.FlowsheetShowConsoleWindow

        Public Property FlowsheetShowCOReportsWindow As Boolean = False Implements Interfaces.IFlowsheetOptions.FlowsheetShowCOReportsWindow

        Public Property FlowsheetShowWatchWindow As Boolean = False Implements Interfaces.IFlowsheetOptions.FlowsheetShowWatchWindow

        Public Property FlowsheetSnapToGrid As Boolean = False Implements Interfaces.IFlowsheetOptions.FlowsheetSnapToGrid

        Public Property FractionNumberFormat As String = "N6" Implements Interfaces.IFlowsheetOptions.FractionNumberFormat

        Public Property Key As String = "" Implements Interfaces.IFlowsheetOptions.Key

        Public Property NumberFormat As String = "N6" Implements Interfaces.IFlowsheetOptions.NumberFormat

        Public Property Password As String = "" Implements Interfaces.IFlowsheetOptions.Password

        Public Property SimulationAuthor As String = "" Implements Interfaces.IFlowsheetOptions.SimulationAuthor

        Public Property SimulationComments As String = "" Implements Interfaces.IFlowsheetOptions.SimulationComments

        Public Property SimulationName As String = "" Implements Interfaces.IFlowsheetOptions.SimulationName

        Public Property UsePassword As Boolean = False Implements Interfaces.IFlowsheetOptions.UsePassword

        Public Property SelectedUnitSystem1 As Interfaces.IUnitsOfMeasure Implements Interfaces.IFlowsheetOptions.SelectedUnitSystem
            Get
                Return Me.SelectedUnitSystem
            End Get
            Set(value As Interfaces.IUnitsOfMeasure)
                Me.SelectedUnitSystem = value
            End Set
        End Property

        Public Property VisibleProperties As New Dictionary(Of String, List(Of String)) Implements Interfaces.IFlowsheetOptions.VisibleProperties

        Public Property SimultaneousAdjustSolverEnabled As Boolean = True Implements Interfaces.IFlowsheetOptions.SimultaneousAdjustSolverEnabled

        Public Property SpreadsheetUseRegionalSeparator As Boolean = False Implements Interfaces.IFlowsheetOptions.SpreadsheetUseRegionalSeparator

    End Class

End Namespace
