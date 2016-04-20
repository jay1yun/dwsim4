'    DWSIM Flowsheet Solver & Auxiliary Functions
'    Copyright 2008-2015 Daniel Wagner O. de Medeiros
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

Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Linq
Imports DWSIM.Interfaces
Imports DWSIM.Interfaces.Enums.GraphicObjects
Imports DWSIM.Interfaces.Enums
Imports DWSIM.GlobalSettings

'custom event handler declaration
Public Delegate Sub CustomEvent(ByVal sender As Object, ByVal e As System.EventArgs, ByVal extrainfo As Object)

<System.Serializable()> Public Class FlowsheetSolver

    'events for plugins
    Public Shared Event UnitOpCalculationStarted As CustomEvent
    Public Shared Event UnitOpCalculationFinished As CustomEvent
    Public Shared Event FlowsheetCalculationStarted As CustomEvent
    Public Shared Event FlowsheetCalculationFinished As CustomEvent
    Public Shared Event MaterialStreamCalculationStarted As CustomEvent
    Public Shared Event MaterialStreamCalculationFinished As CustomEvent

    ''' <summary>
    ''' Flowsheet calculation routine 1. Calculates the object using information sent by the queue and updates the flowsheet.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to calculate (FormChild object).</param>
    ''' <param name="objArgs">A CalculationArgs object containing information about the object to be calculated and its current status.</param>
    ''' <param name="sender"></param>
    ''' <remarks></remarks>
    Public Shared Sub CalculateFlowsheet(ByVal fobj As Object, ByVal objArgs As CalculationArgs, ByVal sender As Object, Optional ByVal OnlyMe As Boolean = False)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        RaiseEvent UnitOpCalculationStarted(fobj, New System.EventArgs(), objArgs)

        'fobj.ProcessScripts(Script.EventType.ObjectCalculationStarted, Script.ObjectType.FlowsheetObject, objArgs.Name)

        Select Case objArgs.ObjectType
            Case ObjectType.MaterialStream
                Dim myObj = fbag.SimulationObjects(objArgs.Name)
                Dim gobj As IGraphicObject = myObj.GraphicObject
                If Not gobj Is Nothing Then
                    If gobj.OutputConnectors(0).IsAttached = True Then
                        Dim myUnitOp = fbag.SimulationObjects(myObj.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Name)
                        If objArgs.Sender = "Spec" Or objArgs.Sender = "FlowsheetSolver" Then
                            CalculateMaterialStream(fobj, myObj, , OnlyMe)
                        Else
                            If objArgs.Calculated = True Then
                                gobj = myUnitOp.GraphicObject
                                gobj.Calculated = True
                                myUnitOp.Calculate()
                                gobj.Status = Status.Calculated
                                If myUnitOp.IsSpecAttached = True And myUnitOp.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(myUnitOp.AttachedSpecId).Calculate()
                                fgui.ShowMessage(gobj.Tag & ": " & fgui.GetTranslatedString("Calculadocomsucesso"), IFlowsheet.MessageType.Information)
                            Else
                                myUnitOp.DeCalculate()
                                gobj = myUnitOp.GraphicObject
                                gobj.Calculated = False
                            End If
                        End If
                    End If
                    If myObj.IsSpecAttached And myObj.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(myObj.AttachedSpecId).Calculate()
                End If
            Case ObjectType.EnergyStream
                Dim myObj = fbag.SimulationObjects(objArgs.Name)
                myObj.Calculated = True
                Dim gobj As IGraphicObject = myObj.GraphicObject
                If Not gobj Is Nothing Then
                    If gobj.OutputConnectors(0).IsAttached = True And Not OnlyMe Then
                        Dim myUnitOp = fbag.SimulationObjects(myObj.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Name)
                        If objArgs.Calculated = True Then
                            myUnitOp.GraphicObject.Calculated = False
                            myUnitOp.Calculate()
                            fgui.ShowMessage(gobj.Tag & ": " & fgui.GetTranslatedString("Calculadocomsucesso"), IFlowsheet.MessageType.Information)
                            myUnitOp.GraphicObject.Calculated = True
                            If myUnitOp.IsSpecAttached = True And myUnitOp.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(myUnitOp.AttachedSpecId).Calculate()
                            gobj = myUnitOp.GraphicObject
                            gobj.Calculated = True
                        Else
                            myUnitOp.DeCalculate()
                            myUnitOp.GraphicObject.Calculated = False
                        End If
                    End If
                    If myObj.IsSpecAttached And myObj.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(myObj.AttachedSpecId).Calculate()
                End If
            Case Else
                If objArgs.Sender = "Adjust" Or objArgs.Sender = "FlowsheetSolver" Then
                    Dim myObj As ISimulationObject = fbag.SimulationObjects(objArgs.Name)
                    myObj.GraphicObject.Calculated = False
                    myObj.Calculate()
                    fgui.ShowMessage(objArgs.Tag & ": " & fgui.GetTranslatedString("Calculadocomsucesso"), IFlowsheet.MessageType.Information)
                    myObj.GraphicObject.Calculated = True
                    If myObj.IsSpecAttached = True And myObj.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(myObj.AttachedSpecId).Calculate()
                Else
                    Dim myObj As ISimulationObject = fbag.SimulationObjects(objArgs.Name)
                    Dim gobj As IGraphicObject = myObj.GraphicObject
                    If Not OnlyMe Then
                        For Each cp As IConnectionPoint In gobj.OutputConnectors
                            If cp.IsAttached And cp.Type = ConType.ConOut Then
                                Dim obj = fbag.SimulationObjects(cp.AttachedConnector.AttachedTo.Name)
                                If obj.GraphicObject.ObjectType = ObjectType.MaterialStream Then
                                    obj.GraphicObject.Calculated = False
                                    obj.Calculate()
                                    obj.GraphicObject.Calculated = True
                                End If
                            End If
                        Next
                    End If
                    If myObj.IsSpecAttached And myObj.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(myObj.AttachedSpecId).Calculate()
                End If
        End Select

        'fobj.ProcessScripts(Script.EventType.ObjectCalculationFinished, Script.ObjectType.FlowsheetObject, objArgs.Name)

        RaiseEvent UnitOpCalculationFinished(fobj, New System.EventArgs(), objArgs)

    End Sub

    ''' <summary>
    ''' Calculates the flowsheet objects asynchronously. This function is always called from a task or a different thread other than UI's.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to calculate (FormChild object).</param>
    ''' <param name="objArgs">A CalculationArgs object containing information about the object to be calculated and its current status.</param>
    ''' <param name="ct">The cancellation token, used to listen for calculation cancellation requests from the user.</param>
    ''' <remarks></remarks>
    Public Shared Sub CalculateFlowsheetAsync(ByVal fobj As Object, ByVal objArgs As CalculationArgs, ct As Threading.CancellationToken)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        If ct.IsCancellationRequested = True Then ct.ThrowIfCancellationRequested()

        If objArgs.Sender = "FlowsheetSolver" Then
            'fobj.ProcessScripts(Script.EventType.ObjectCalculationStarted, Script.ObjectType.FlowsheetObject, objArgs.Name)
            Select Case objArgs.ObjectType
                Case ObjectType.MaterialStream
                    Dim myObj = fbag.SimulationObjects(objArgs.Name)
                    RaiseEvent MaterialStreamCalculationStarted(fobj, New System.EventArgs(), myObj)
                    CalculateMaterialStreamAsync(fobj, myObj, ct)
                    If myObj.IsSpecAttached And myObj.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(myObj.AttachedSpecId).Calculate()
                    RaiseEvent MaterialStreamCalculationFinished(fobj, New System.EventArgs(), myObj)
                Case ObjectType.EnergyStream
                    Dim myObj = fbag.SimulationObjects(objArgs.Name)
                    If myObj.IsSpecAttached And myObj.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(myObj.AttachedSpecId).Calculate()
                    myObj.Calculated = True
                Case Else
                    Dim myObj As ISimulationObject = fbag.SimulationObjects(objArgs.Name)
                    RaiseEvent UnitOpCalculationStarted(fobj, New System.EventArgs(), objArgs)
                    myObj.Calculate()
                    If myObj.IsSpecAttached And myObj.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(myObj.AttachedSpecId).Calculate()
                    RaiseEvent UnitOpCalculationFinished(fobj, New System.EventArgs(), objArgs)
            End Select
            'fobj.ProcessScripts(Script.EventType.ObjectCalculationFinished, Script.ObjectType.FlowsheetObject, objArgs.Name)
        End If

    End Sub

    ''' <summary>
    ''' Material Stream calculation routine 1. This routine check all input values and calculates all remaining properties of the stream.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to what the stream belongs to.</param>
    ''' <param name="ms">Material Stream object to be calculated.</param>
    ''' <param name="DoNotCalcFlash">Tells the calculator whether to do flash calculations or not.</param>
    ''' <remarks></remarks>
    Public Shared Sub CalculateMaterialStream(ByVal fobj As Object, ByVal ms As ISimulationObject, Optional ByVal DoNotCalcFlash As Boolean = False, Optional ByVal OnlyMe As Boolean = False)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        ms.Calculated = False

        RaiseEvent MaterialStreamCalculationStarted(fobj, New System.EventArgs(), ms)

        ' fobj.ProcessScripts(Script.EventType.ObjectCalculationStarted, Script.ObjectType.FlowsheetObject, ms.Name)

        ms.GraphicObject.Calculated = False

        ms.Calculate()

        fgui.ShowMessage(ms.GraphicObject.Tag & ": " & fgui.GetTranslatedString("Calculadocomsucesso"), IFlowsheet.MessageType.Information)

        'fobj.ProcessScripts(Script.EventType.ObjectCalculationFinished, Script.ObjectType.FlowsheetObject, ms.Name)

        RaiseEvent MaterialStreamCalculationFinished(fobj, New System.EventArgs(), ms)

        ms.LastUpdated = Date.Now
        ms.Calculated = True

        If Not OnlyMe Then
            Dim objargs As New CalculationArgs
            With objargs
                .Calculated = True
                .Name = ms.Name
                .ObjectType = ObjectType.MaterialStream
            End With
            CalculateFlowsheet(fobj, objargs, Nothing)
        End If

    End Sub

    ''' <summary>
    ''' Calculates a material stream object asynchronously. This function is always called from a task or a different thread other than UI's.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to what the stream belongs to.</param>
    ''' <param name="ms">Material Stream object to be calculated.</param>
    ''' <param name="ct">The cancellation token, used to listen for calculation cancellation requests from the user.</param>
    ''' <remarks></remarks>
    Public Shared Sub CalculateMaterialStreamAsync(ByVal fobj As Object, ByVal ms As ISimulationObject, ct As Threading.CancellationToken)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        If ct.IsCancellationRequested = True Then ct.ThrowIfCancellationRequested()

        ms.Calculated = False

        RaiseEvent MaterialStreamCalculationStarted(fobj, New System.EventArgs(), ms)

        'fobj.ProcessScripts(Script.EventType.ObjectCalculationStarted, Script.ObjectType.FlowsheetObject, ms.Name)

        ms.Calculate()

        'fobj.ProcessScripts(Script.EventType.ObjectCalculationFinished, Script.ObjectType.FlowsheetObject, ms.Name)

        RaiseEvent MaterialStreamCalculationFinished(fobj, New System.EventArgs(), ms)

        If ms.IsSpecAttached = True And ms.SpecVarType = SpecVarType.Source Then fbag.SimulationObjects(ms.AttachedSpecId).Calculate()

        ms.LastUpdated = Date.Now
        ms.Calculated = True

    End Sub

    ''' <summary>
    ''' Process the calculation queue of the Flowsheet passed as an argument. Checks all elements in the queue and calculates them.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to be calculated (FormChild object)</param>
    ''' <remarks></remarks>
    Public Shared Function ProcessCalculationQueue(ByVal fobj As Object, Optional ByVal Isolated As Boolean = False,
                                              Optional ByVal FlowsheetSolverMode As Boolean = False,
                                              Optional ByVal mode As Integer = 0,
                                              Optional orderedlist As Object = Nothing,
                                              Optional ByVal ct As Threading.CancellationToken = Nothing,
                                              Optional ByVal Adjusting As Boolean = False) As List(Of Exception)

        Dim exlist As New List(Of Exception)

        If mode = 0 Then
            'UI thread
            exlist = ProcessQueueInternal(fobj, Isolated, FlowsheetSolverMode, ct)
            If Not Adjusting Then SolveSimultaneousAdjusts(fobj)
        ElseIf mode = 1 Then
            'bg thread
            exlist = ProcessQueueInternalAsync(fobj, ct)
            If Not Adjusting Then SolveSimultaneousAdjustsAsync(fobj, ct)
        ElseIf mode = 2 Then
            'bg parallel thread
            'Dim prevset As Boolean = My.Settings.EnableParallelProcessing
            'My.Settings.EnableParallelProcessing = False
            exlist = ProcessQueueInternalAsyncParallel(fobj, orderedlist, ct)
            If Not Adjusting Then SolveSimultaneousAdjustsAsync(fobj, ct)
            'My.Settings.EnableParallelProcessing = prevset
        End If

        Return exlist

    End Function

    ''' <summary>
    ''' This is the internal routine called by ProcessCalculationQueue when the UI thread is used to calculate the flowsheet.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to be calculated (FormChild object)</param>
    ''' <param name="Isolated">Tells to the calculator that only the objects in the queue must be calculated without checking the outlet connections, that is, no more objects will be added to the queue</param>
    ''' <param name="FlowsheetSolverMode">Only objects added by the flowsheet solving routine to the queue will be calculated.</param>
    ''' <param name="ct">The cancellation token, used to listen for calculation cancellation requests from the user.</param>
    ''' <remarks></remarks>
    Private Shared Function ProcessQueueInternal(ByVal fobj As Object, Optional ByVal Isolated As Boolean = False, Optional ByVal FlowsheetSolverMode As Boolean = False, Optional ByVal ct As Threading.CancellationToken = Nothing) As List(Of Exception)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        Dim d0 As Date = Date.Now

        Dim loopex As New List(Of Exception)

        While fqueue.CalculationQueue.Count >= 1

            If ct.IsCancellationRequested = True Then ct.ThrowIfCancellationRequested()

            Dim myinfo As CalculationArgs = fqueue.CalculationQueue.Peek()

            If fbag.SimulationObjects.ContainsKey(myinfo.Name) Then

                Dim myobj = fbag.SimulationObjects(myinfo.Name)
                Try
                    myobj.ErrorMessage = ""
                    If myobj.GraphicObject.Active Then
                        If FlowsheetSolverMode Then
                            If myinfo.Sender = "FlowsheetSolver" Then
                                If myinfo.ObjectType = ObjectType.MaterialStream Then
                                    CalculateMaterialStream(fobj, fbag.SimulationObjects(myinfo.Name), , Isolated)
                                Else
                                    CalculateFlowsheet(fobj, myinfo, Nothing, Isolated)
                                End If
                            End If
                        Else
                            If myinfo.ObjectType = ObjectType.MaterialStream Then
                                CalculateMaterialStream(fobj, fbag.SimulationObjects(myinfo.Name), , Isolated)
                            Else
                                CalculateFlowsheet(fobj, myinfo, Nothing, Isolated)
                            End If
                        End If
                        myobj.GraphicObject.Calculated = True
                    End If
                Catch ex As AggregateException
                    myobj.ErrorMessage = ""
                    For Each iex In ex.InnerExceptions
                        myobj.ErrorMessage += iex.Message.ToString & vbCrLf
                        loopex.Add(New Exception(myinfo.Tag & ": " & iex.Message))
                    Next
                    'If My.Settings.SolverBreakOnException Then Exit While
                Catch ex As Exception
                    myobj.ErrorMessage = ex.Message.ToString & vbCrLf
                    loopex.Add(New Exception(myinfo.Tag & ": " & ex.Message))
                    'If My.Settings.SolverBreakOnException Then Exit While
                End Try

            End If

            CheckCalculatorStatus()

            If fqueue.CalculationQueue.Count > 0 Then fqueue.CalculationQueue.Dequeue()

        End While

        fgui.ShowMessage(fgui.GetTranslatedString("Runtime") & ": " & (Date.Now - d0).ToString, IFlowsheet.MessageType.Information)
        
        Return loopex

    End Function

    ''' <summary>
    ''' This is the internal routine called by ProcessCalculationQueue when a background thread is used to calculate the flowsheet.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to be calculated (FormChild object)</param>
    ''' <param name="ct">The cancellation token, used to listen for calculation cancellation requests from the user.</param>
    ''' <remarks></remarks>
    Private Shared Function ProcessQueueInternalAsync(ByVal fobj As Object, ByVal ct As Threading.CancellationToken) As List(Of Exception)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        Dim d0 As Date = Date.Now

        Dim loopex As New List(Of Exception)

        While fqueue.CalculationQueue.Count >= 1

            If ct.IsCancellationRequested = True Then ct.ThrowIfCancellationRequested()

            Dim myinfo As CalculationArgs = fqueue.CalculationQueue.Peek()

            'fobj.UIThread(Sub() UpdateDisplayStatus(fobj, New String() {myinfo.Name}, True))
            Dim myobj = fbag.SimulationObjects(myinfo.Name)
            Try
                myobj.ErrorMessage = ""
                If myobj.GraphicObject.Active Then
                    If myinfo.ObjectType = ObjectType.MaterialStream Then
                        CalculateMaterialStreamAsync(fobj, myobj, ct)
                    Else
                        CalculateFlowsheetAsync(fobj, myinfo, ct)
                    End If
                    myobj.GraphicObject.Calculated = True
                End If
            Catch ex As AggregateException
                'fobj.ProcessScripts(Script.EventType.ObjectCalculationError, Script.ObjectType.FlowsheetObject, myobj.Name)
                myobj.ErrorMessage = ""
                For Each iex In ex.InnerExceptions
                    myobj.ErrorMessage += iex.Message.ToString & vbCrLf
                    loopex.Add(New Exception(myinfo.Tag & ": " & iex.Message))
                Next
                'If My.Settings.SolverBreakOnException Then Exit While
            Catch ex As Exception
                'fobj.ProcessScripts(Script.EventType.ObjectCalculationError, Script.ObjectType.FlowsheetObject, myobj.Name)
                myobj.ErrorMessage = ex.Message.ToString
                loopex.Add(New Exception(myinfo.Tag & ": " & ex.Message))
                'If My.Settings.SolverBreakOnException Then Exit While
            Finally
                'fobj.UIThread(Sub() UpdateDisplayStatus(fobj, New String() {myinfo.Name}))
            End Try

            If fqueue.CalculationQueue.Count > 0 Then fqueue.CalculationQueue.Dequeue()

        End While

        Return loopex

    End Function

    ''' <summary>
    ''' This is the internal routine called by ProcessCalculationQueue when background parallel threads are used to calculate the flowsheet.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to be calculated (FormChild object)</param>
    ''' <param name="ct">The cancellation token, used to listen for calculation cancellation requests from the user.</param>
    ''' <remarks></remarks>
    Private Shared Function ProcessQueueInternalAsyncParallel(ByVal fobj As Object, ByVal orderedlist As Dictionary(Of Integer, List(Of CalculationArgs)), ct As Threading.CancellationToken) As List(Of Exception)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        Dim loopex As New Concurrent.ConcurrentBag(Of Exception)

        For Each obj In fbag.SimulationObjects.Values
            If TypeOf obj Is ISimulationObject Then
                DirectCast(obj, ISimulationObject).PropertyPackage = Nothing
                DirectCast(obj, ISimulationObject).PropertyPackage = DirectCast(obj, ISimulationObject).PropertyPackage.Clone
            ElseIf TypeOf obj Is IMaterialStream Then
                DirectCast(obj, ISimulationObject).PropertyPackage = Nothing
                DirectCast(obj, ISimulationObject).PropertyPackage = DirectCast(obj, ISimulationObject).PropertyPackage.Clone
                DirectCast(obj, ISimulationObject).PropertyPackage.CurrentMaterialStream = obj
            End If
        Next

        Dim poptions As New ParallelOptions() With {.MaxDegreeOfParallelism = Settings.MaxDegreeOfParallelism,
                                                    .TaskScheduler = Settings.AppTaskScheduler}

        For Each li In orderedlist
            Dim objlist As New ArrayList
            For Each item In li.Value
                objlist.Add(item.Name)
            Next
            Parallel.ForEach(li.Value, poptions, Sub(myinfo, state)
                                                     If ct.IsCancellationRequested = True Then ct.ThrowIfCancellationRequested()
                                                     Dim myobj = fbag.SimulationObjects(myinfo.Name)
                                                     myobj.ErrorMessage = ""
                                                     Try
                                                         If myobj.GraphicObject.Active Then
                                                             If myinfo.ObjectType = ObjectType.MaterialStream Then
                                                                 CalculateMaterialStreamAsync(fobj, myobj, ct)
                                                             Else
                                                                 CalculateFlowsheetAsync(fobj, myinfo, ct)
                                                             End If
                                                             myobj.GraphicObject.Calculated = True
                                                         End If
                                                     Catch ex As AggregateException
                                                         'fobj.ProcessScripts(Script.EventType.ObjectCalculationError, Script.ObjectType.FlowsheetObject, myobj.Name)
                                                         myobj.ErrorMessage = ""
                                                         For Each iex In ex.InnerExceptions
                                                             myobj.ErrorMessage += iex.Message.ToString & vbCrLf
                                                             loopex.Add(New Exception(myinfo.Tag & ": " & iex.Message))
                                                         Next
                                                         'If My.Settings.SolverBreakOnException Then state.Break()
                                                     Catch ex As Exception
                                                         'fobj.ProcessScripts(Script.EventType.ObjectCalculationError, Script.ObjectType.FlowsheetObject, myobj.Name)
                                                         myobj.ErrorMessage = ex.Message.ToString
                                                         loopex.Add(New Exception(myinfo.Tag & ": " & ex.Message))
                                                         'If My.Settings.SolverBreakOnException Then state.Break()
                                                     Finally
                                                         'fobj.UIThread(Sub() UpdateDisplayStatus(fobj, New String() {myinfo.Name}))
                                                     End Try
                                                 End Sub)
        Next

        For Each obj In fbag.SimulationObjects.Values
            If TypeOf obj Is ISimulationObject Then
                DirectCast(obj, ISimulationObject).PropertyPackage = Nothing
            ElseIf TypeOf obj Is IMaterialStream Then
                DirectCast(obj, ISimulationObject).PropertyPackage = Nothing
            End If
        Next

        Return loopex.ToList

    End Function

    ''' <summary>
    ''' Checks the calculator status to see if the user did any stop/abort request, and throws an exception to force aborting, if necessary.
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared Sub CheckCalculatorStatus()
        If Not Settings.CAPEOPENMode Then
            'If My.Application.CalculatorStopRequested = True Then
            '    My.Application.MasterCalculatorStopRequested = True
            '    My.Application.CalculatorStopRequested = False
            '    If Settings.TaskCancellationTokenSource IsNot Nothing Then
            '        If Not Settings.TaskCancellationTokenSource.IsCancellationRequested Then
            '            Settings.TaskCancellationTokenSource.Cancel()
            '        End If
            '        Settings.TaskCancellationTokenSource.Token.ThrowIfCancellationRequested()
            '    Else
            '        Throw New Exception(fgui.GetTranslatedString("CalculationAborted"))
            '    End If
            'End If
        End If
    End Sub

    ''' <summary>
    ''' This routine updates the display status of a list of graphic objects in the flowsheet according to their calculated status.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to be calculated (FormChild object).</param>
    ''' <param name="ObjIDlist">List of object IDs to be updated.</param>
    ''' <param name="calculating">Tell the routine that the objects in the list are being calculated at the moment.</param>
    ''' <remarks></remarks>
    Shared Sub UpdateDisplayStatus(fobj As Object, Optional ByVal ObjIDlist() As String = Nothing, Optional ByVal calculating As Boolean = False)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        If ObjIDlist Is Nothing Then
            For Each baseobj In fbag.SimulationObjects.Values
                If Not baseobj.GraphicObject Is Nothing Then
                    If Not baseobj.GraphicObject.Active Then
                        baseobj.GraphicObject.Status = Status.Inactive
                    Else
                        baseobj.GraphicObject.Calculated = baseobj.Calculated
                        'If baseobj.Calculated Then baseobj.UpdatePropertyNodes(fobj.Options.SelectedUnitSystem, fobj.Options.NumberFormat)
                    End If
                End If
            Next
        Else
            For Each ObjID In ObjIDlist
                If fbag.SimulationObjects.ContainsKey(ObjID) Then
                    Dim baseobj = fbag.SimulationObjects(ObjID)
                    If Not baseobj.GraphicObject Is Nothing Then
                        If calculating Then
                            baseobj.GraphicObject.Status = Status.Calculating
                        Else
                            If Not baseobj.GraphicObject.Active Then
                                baseobj.GraphicObject.Status = Status.Inactive
                            Else
                                baseobj.GraphicObject.Calculated = baseobj.Calculated
                                'If baseobj.Calculated Then baseobj.UpdatePropertyNodes(fobj.Options.SelectedUnitSystem, fobj.Options.NumberFormat)
                            End If
                        End If
                    End If
                End If
            Next
        End If
        'fobj.UIThread(Sub()
        '                  fobj.FormSurface.FlowsheetDesignSurface.Invalidate()
        '              End Sub)

    End Sub

    ''' <summary>
    ''' Retrieves the list of objects to be solved in the flowsheet.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to be calculated (FormChild object)</param>
    ''' <param name="frompgrid">Starts the search from the edited object if the propert was changed from the property grid.</param>
    ''' <returns>A list of objects to be calculated in the flowsheet.</returns>
    ''' <remarks></remarks>
    Private Shared Function GetSolvingList(fobj As Object, frompgrid As Boolean) As Object()

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        Dim obj As ISimulationObject

        Dim lists As New Dictionary(Of Integer, List(Of String))
        Dim filteredlist As New Dictionary(Of Integer, List(Of String))
        Dim objstack As New List(Of String)

        Dim onqueue As CalculationArgs = Nothing

        Dim listidx As Integer = 0
        Dim maxidx As Integer = 0

        If frompgrid Then

            If fqueue.CalculationQueue.Count > 0 Then

                onqueue = fqueue.CalculationQueue.Dequeue()
                fqueue.CalculationQueue.Clear()

                lists.Add(0, New List(Of String))

                lists(0).Add(onqueue.Name)

                'now start walking through the flowsheet until it reaches its end starting from this particular object.

                Do
                    listidx += 1
                    If lists(listidx - 1).Count > 0 Then
                        lists.Add(listidx, New List(Of String))
                        maxidx = listidx
                        For Each o As String In lists(listidx - 1)
                            obj = fbag.SimulationObjects(o)
                            If obj.GraphicObject.Active Then
                                For Each c As IConnectionPoint In obj.GraphicObject.OutputConnectors
                                    If c.IsAttached Then
                                        If obj.GraphicObject.ObjectType = ObjectType.OT_Recycle Or obj.GraphicObject.ObjectType = ObjectType.OT_EnergyRecycle Then Exit For
                                        lists(listidx).Add(c.AttachedConnector.AttachedTo.Name)
                                    End If
                                Next
                            End If
                        Next
                    Else
                        Exit Do
                    End If
                    If lists.Count > 10000 Then
                        lists.Clear()
                        Throw New Exception("Infinite loop detected while obtaining flowsheet object calculation order. Please insert recycle blocks where needed.")
                    End If
                Loop

                'process the lists , adding objects to the stack, discarding duplicate entries.

                listidx = 0

                Do
                    If lists.ContainsKey(listidx) Then
                        filteredlist.Add(listidx, New List(Of String)(lists(listidx).ToArray))
                        For Each o As String In lists(listidx)
                            If Not objstack.Contains(o) Then
                                objstack.Add(o)
                            Else
                                filteredlist(listidx).Remove(o)
                            End If
                        Next
                    Else
                        Exit Do
                    End If
                    listidx += 1
                Loop Until listidx > maxidx

            End If

        Else

            'add endpoint material streams and recycle ops to the list, they will be the last objects to be calculated.

            lists.Add(0, New List(Of String))

            For Each baseobj In fbag.SimulationObjects.Values
                If baseobj.GraphicObject.ObjectType = ObjectType.MaterialStream Then
                    If baseobj.GraphicObject.OutputConnectors(0).IsAttached = False Then
                        lists(0).Add(baseobj.Name)
                    End If
                ElseIf baseobj.GraphicObject.ObjectType = ObjectType.EnergyStream Then
                    lists(0).Add(baseobj.Name)
                ElseIf baseobj.GraphicObject.ObjectType = ObjectType.OT_Recycle Then
                    lists(0).Add(baseobj.Name)
                ElseIf baseobj.GraphicObject.ObjectType = ObjectType.OT_EnergyRecycle Then
                    lists(0).Add(baseobj.Name)
                End If
            Next

            'now start processing the list at each level, until it reaches the beginning of the flowsheet.

            Do
                listidx += 1
                If lists(listidx - 1).Count > 0 Then
                    lists.Add(listidx, New List(Of String))
                    maxidx = listidx
                    For Each o As String In lists(listidx - 1)
                        obj = fbag.SimulationObjects(o)
                        If Not onqueue Is Nothing Then
                            If onqueue.Name = obj.Name Then Exit Do
                        End If
                        For Each c As IConnectionPoint In obj.GraphicObject.InputConnectors
                            If c.IsAttached Then
                                If c.AttachedConnector.AttachedFrom.ObjectType <> ObjectType.OT_Recycle And
                                    c.AttachedConnector.AttachedFrom.ObjectType <> ObjectType.OT_EnergyRecycle Then
                                    lists(listidx).Add(c.AttachedConnector.AttachedFrom.Name)
                                End If
                            End If
                        Next
                    Next
                Else
                    Exit Do
                End If
                If lists.Count > 10000 Then
                    lists.Clear()
                    Throw New Exception("Infinite loop detected while obtaining flowsheet object calculation order. Please insert recycle blocks where needed.")
                End If
            Loop

            'process the lists backwards, adding objects to the stack, discarding duplicate entries.

            listidx = maxidx

            Do
                If lists.ContainsKey(listidx) Then
                    filteredlist.Add(maxidx - listidx, New List(Of String)(lists(listidx).ToArray))
                    For Each o As String In lists(listidx)
                        If Not objstack.Contains(o) Then
                            objstack.Add(o)
                        Else
                            filteredlist(maxidx - listidx).Remove(o)
                        End If
                    Next
                Else
                    Exit Do
                End If
                listidx -= 1
            Loop

        End If

        Dim speclist = (From s In fbag.SimulationObjects.Values Select s Where s.GraphicObject.ObjectType = ObjectType.OT_Spec).ToArray

        If speclist.Count > 0 Then
            Dim newstack As New List(Of String)
            For Each o In objstack
                newstack.Add(o)
                obj = fbag.SimulationObjects(o)
                'if the object has a spec attached to it, set the destination object to be calculated after it.
                If obj.IsSpecAttached And obj.SpecVarType = SpecVarType.Source Then
                    'newstack.Add(fbag.SimulationObjects(obj.AttachedSpecId).TargetObjectData.m_ID)
                End If
            Next
            Dim newfilteredlist As New Dictionary(Of Integer, List(Of String))
            For Each kvp In filteredlist
                Dim newlist As New List(Of String)
                For Each o In kvp.Value
                    newlist.Add(o)
                    obj = fbag.SimulationObjects(o)
                    'if the object has a spec attached to it, set the destination object to be calculated after it.
                    If obj.IsSpecAttached And obj.SpecVarType = SpecVarType.Source Then
                        'newlist.Add(fbag.SimulationObjects(obj.AttachedSpecId).TargetO.m_ID)
                    End If
                Next
                newfilteredlist.Add(kvp.Key, newlist)
            Next
            Return New Object() {newstack, lists, newfilteredlist}
        End If

        Return New Object() {objstack, lists, filteredlist}

    End Function

    ''' <summary>
    ''' Calculate all objects in the Flowsheet using a ordering method.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to be calculated (FormFlowsheet object).</param>
    ''' <param name="Adjusting">True if the routine is called from the Simultaneous Adjust Solver.</param>
    ''' <param name="frompgrid">True if the routine is called from a PropertyGrid PropertyChanged event.</param>
    ''' <param name="mode">0 = Main Thread, 1 = Background Thread, 2 = Background Parallel Threads, 3 = Azure Service Bus, 4 = Network Computer</param>
    ''' <param name="ts">CancellationTokenSource instance from main flowsheet when calculating subflowsheets.</param>
    ''' <remarks></remarks>
    Public Shared Sub SolveFlowsheet(ByVal fobj As Object, mode As Integer, Optional ByVal ts As CancellationTokenSource = Nothing, Optional frompgrid As Boolean = False, Optional Adjusting As Boolean = False)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        'checks if the calculator is activated.

        'If fobj.MasterFlowsheet Is Nothing Then My.Application.CalculatorBusy = True

        'this is the cancellation token for background threads. it checks for calculator stop requests and forwards the request to the tasks.

        'If fobj.MasterFlowsheet Is Nothing Then
        If ts Is Nothing Then ts = New CancellationTokenSource
        Settings.TaskCancellationTokenSource = ts
        'End If
        Dim ct As CancellationToken = Settings.TaskCancellationTokenSource.Token

        Dim obj As ISimulationObject

        'mode:
        '0 = Synchronous (main thread)
        '1 = Asynchronous (background thread)
        '2 = Asynchronous Parallel (background thread)
        '3 = Azure Service Bus
        '4 = Network Computer

        Dim d1 As Date = Date.Now
        Dim age As AggregateException = Nothing
        Dim exlist As New List(Of Exception)

        'gets a list of objects to be solved in the flowsheet

        Dim objl As Object() = GetSolvingList(fobj, frompgrid)

        'declare a filteredlist dictionary. this will hold the sequence of grouped objects that can be calculated 
        'this way if the user selects the background parallel threads solver option

        Dim filteredlist2 As New Dictionary(Of Integer, List(Of CalculationArgs))

        'assign the list of objects, the filtered list (which contains no duplicate elements) and the object stack
        'which contains the ordered list of objects to be calculated.

        Dim lists As Dictionary(Of Integer, List(Of String)) = objl(1)
        Dim filteredlist As Dictionary(Of Integer, List(Of String)) = objl(2)
        Dim objstack As List(Of String) = objl(0)

        If objstack.Count = 0 Then Exit Sub

        'adds a message to the log window to indicate that the flowsheet started solving

        fgui.ShowMessage(fgui.GetTranslatedString("FSstartedsolving"), IFlowsheet.MessageType.Information)

        'process scripts associated with the solverstarted event

        'fobj.ProcessScripts(Script.EventType.SolverStarted, Script.ObjectType.Solver)

        RaiseEvent FlowsheetCalculationStarted(fobj, New System.EventArgs(), Nothing)

        'find recycles

        Dim recycles As New List(Of String)
        Dim totalv As Integer = 0

        For Each r In objstack
            Dim robj = fbag.SimulationObjects(r)
            If robj.GraphicObject.ObjectType = ObjectType.OT_Recycle Then
                recycles.Add(robj.Name)
                Dim rec As IRecycle = fbag.SimulationObjects(robj.Name)
                If rec.AccelerationMethod = AccelMethod.GlobalBroyden Then
                    If rec.Values.Count = 0 Then fbag.SimulationObjects(robj.Name).Calculate()
                    totalv += rec.Values.Count
                End If
            End If
        Next

        'size hessian matrix, variables and error vectors for recycle simultaneous solving.

        Dim rechess(totalv - 1, totalv - 1), recvars(totalv - 1), recdvars(totalv - 1), recerrs(totalv - 1), recvarsb(totalv - 1), recerrsb(totalv - 1) As Double

        'identity matrix as first hessian.

        For i As Integer = 0 To totalv - 1
            rechess(i, i) = 1
        Next

        'set all objects' status to 'not calculated' (red) in the list

        For Each o In objstack
            obj = fbag.SimulationObjects(o)
            With obj
                .Calculated = False
                If Not obj.GraphicObject Is Nothing Then
                    If obj.GraphicObject.Active Then
                        obj.GraphicObject.Calculated = False
                    Else
                        obj.GraphicObject.Status = Status.Inactive
                    End If
                End If
            End With
        Next

        'initialize GPU if option enabled

        If Settings.EnableGPUProcessing Then
            'Calculator.InitComputeDevice()
            Settings.gpu.EnableMultithreading()
        End If

        Select Case mode

            Case 0, 1, 2

                '0 = main thread, 1 = bg thread, 2 = bg parallel threads

                'define variable to check for flowsheet convergence if there are recycle ops

                Dim converged As Boolean = False

                Dim loopidx As Integer = 0

                'process/calculate the queue.

                If fqueue.CalculationQueue Is Nothing Then fqueue.CalculationQueue = New Queue(Of ICalculationArgs)

                'My.Application.MasterCalculatorStopRequested = False

                Dim objargs As CalculationArgs = Nothing

                Dim maintask As New Task(Sub()

                                             Dim icount As Integer = 0

                                             While Not converged

                                                 'add the objects to the calculation queue.

                                                 For Each o As String In objstack
                                                     obj = fbag.SimulationObjects(o)
                                                     objargs = New CalculationArgs
                                                     With objargs
                                                         .Sender = "FlowsheetSolver"
                                                         .Calculated = True
                                                         .Name = obj.Name
                                                         .ObjectType = obj.GraphicObject.ObjectType
                                                         .Tag = obj.GraphicObject.Tag
                                                         fqueue.CalculationQueue.Enqueue(objargs)
                                                     End With
                                                 Next

                                                 'set the flowsheet instance for all objects, this is required for the async threads

                                                 For Each o In fbag.SimulationObjects.Values
                                                     o.SetFlowsheet(fobj)
                                                 Next

                                                 If mode = 0 Then

                                                     exlist = ProcessCalculationQueue(fobj, True, True, 0, Nothing, ct, Adjusting)

                                                 ElseIf mode = 1 Or mode = 2 Then

                                                     filteredlist2.Clear()

                                                     For Each li In filteredlist
                                                         Dim objcalclist As New List(Of CalculationArgs)
                                                         For Each o In li.Value
                                                             obj = fbag.SimulationObjects(o)
                                                             objcalclist.Add(New CalculationArgs() With {.Sender = "FlowsheetSolver", .Name = obj.Name, .ObjectType = obj.GraphicObject.ObjectType, .Tag = obj.GraphicObject.Tag})
                                                         Next
                                                         filteredlist2.Add(li.Key, objcalclist)
                                                     Next

                                                     exlist = ProcessCalculationQueue(fobj, True, True, mode, filteredlist2, ct, Adjusting)

                                                 End If

                                                 'throws exceptions if any

                                                 'If My.Settings.SolverBreakOnException And exlist.Count > 0 Then Throw New AggregateException(exlist)

                                                 'checks for recycle convergence.

                                                 converged = True
                                                 For Each r As String In recycles
                                                     obj = fbag.SimulationObjects(r)
                                                     converged = DirectCast(obj, IRecycle).Converged
                                                     If Not converged Then Exit For
                                                 Next

                                                 If Not converged Then

                                                     Dim avgerr As Double = 0.0#
                                                     Dim rcount As Integer = 0

                                                     For Each r As String In recycles
                                                         obj = fbag.SimulationObjects(r)
                                                         With DirectCast(obj, IRecycle)
                                                             avgerr += 0.33 * .ConvergenceHistory.TemperaturaE / .ConvergenceHistory.Temperatura
                                                             avgerr += 0.33 * .ConvergenceHistory.PressaoE / .ConvergenceHistory.Pressao
                                                             avgerr += 0.33 * .ConvergenceHistory.VazaoMassicaE / .ConvergenceHistory.VazaoMassica
                                                         End With
                                                         rcount += 1
                                                     Next

                                                     avgerr *= 100
                                                     avgerr /= rcount

                                                     fgui.ShowMessage("Recycle loop #" & (icount + 1) & ", average recycle error: " & Format(avgerr, "N") & "%", IFlowsheet.MessageType.Information)

                                                 End If

                                                 'process the scripts associated with the recycle loop event.

                                                 'fobj.ProcessScripts(Script.EventType.SolverRecycleLoop, Script.ObjectType.Solver)

                                                 'if the all recycles have converged (if any), then exit the loop.

                                                 If converged Then

                                                     Exit While

                                                 Else

                                                     If totalv > 0 Then

                                                         'update variables of all recycles set to global broyden.

                                                         Dim i As Integer = 0
                                                         For Each r As String In recycles
                                                             Dim rec = DirectCast(fbag.SimulationObjects(r), IRecycle)
                                                             If rec.AccelerationMethod = AccelMethod.GlobalBroyden Then
                                                                 For Each kvp In rec.Values
                                                                     recvars(i) = kvp.Value
                                                                     recerrs(i) = rec.Errors(kvp.Key)
                                                                     i += 1
                                                                 Next
                                                             End If
                                                         Next

                                                         MathEx.Broyden.broydn(totalv - 1, recvars, recerrs, recdvars, recvarsb, recerrsb, rechess, If(icount < 2, 0, 1))

                                                         i = 0
                                                         For Each r As String In recycles
                                                             Dim rec = DirectCast(fbag.SimulationObjects(r), IRecycle)
                                                             If rec.AccelerationMethod = AccelMethod.GlobalBroyden Then
                                                                 For Each kvp In rec.Errors
                                                                     rec.Values(kvp.Key) = recvars(i) + recdvars(i)
                                                                     i += 1
                                                                 Next
                                                             End If
                                                             rec.SetOutletStreamProperties()
                                                         Next

                                                     End If

                                                 End If

                                                 If frompgrid Then
                                                     objl = GetSolvingList(fobj, False)
                                                     lists = objl(1)
                                                     filteredlist = objl(2)
                                                     objstack = objl(0)
                                                 End If

                                                 icount += 1



                                             End While

                                         End Sub)

                'configure the task scheduler

                Dim nthreads As Integer = Settings.MaxThreadMultiplier * System.Environment.ProcessorCount

                Select Case Settings.TaskScheduler
                    Case 0 'default
                        If Settings.EnableGPUProcessing Then
                            Settings.AppTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext
                        Else
                            Settings.AppTaskScheduler = TaskScheduler.Default
                        End If
                    Case 1 'sta
                        Settings.AppTaskScheduler = New TaskSchedulers.StaTaskScheduler(nthreads)
                    Case 2 'limited concurrency
                        Settings.AppTaskScheduler = New TaskSchedulers.LimitedConcurrencyLevelTaskScheduler(nthreads)
                End Select

                Try
                    If mode = 0 Then
                        'this task will run synchronously with the UI thread.
                        maintask.RunSynchronously(Settings.AppTaskScheduler)
                    Else
                        'fobj.UpdateStatusLabel(fgui.GetTranslatedString("Calculando") & " " & fgui.GetTranslatedString("Fluxograma") & "...")
                        'this task will run asynchronously.
                        maintask.Start(Settings.AppTaskScheduler)
                        While Not (Date.Now - d1).TotalMilliseconds >= Settings.SolverTimeoutSeconds * 1000
                            maintask.Wait(500, ct)
                            'Application.DoEvents()
                            If maintask.Status = TaskStatus.RanToCompletion Then Exit While
                        End While
                        If maintask.Status = TaskStatus.Running Then Throw New TimeoutException(fgui.GetTranslatedString("SolverTimeout"))
                    End If
                    If maintask.IsFaulted Then Throw maintask.Exception
                    If exlist.Count > 0 Then Throw New AggregateException(exlist)
                Catch agex As AggregateException
                    age = agex
                Catch ex As OperationCanceledException
                    age = New AggregateException(fgui.GetTranslatedString("CalculationAborted"), ex)
                Catch ex As Exception
                    age = New AggregateException(ex.Message.ToString, ex)
                Finally
                    If maintask.IsCompleted Then maintask.Dispose()
                    maintask = Nothing
                End Try

                'clears the calculation queue.

                fqueue.CalculationQueue.Clear()

                'disposes the cancellation token source.

                'If fobj.Visible Then ts.Dispose()

                'Settings.TaskCancellationTokenSource = Nothing

                'clears the object lists.

                objstack.Clear()
                lists.Clear()
                recycles.Clear()

            Case 3

                'Azure Service Bus

                Dim azureclient As New AzureSolverClient()

                Try
                    azureclient.SolveFlowsheet(fobj)
                    For Each baseobj In fbag.SimulationObjects.Values
                        If baseobj.Calculated Then baseobj.LastUpdated = Date.Now
                    Next
                Catch ex As Exception
                    age = New AggregateException(ex.Message.ToString, ex)
                Finally
                    If Not azureclient.qcc.IsClosed Then azureclient.qcc.Close()
                    If Not azureclient.qcs.IsClosed Then azureclient.qcs.Close()
                End Try

                azureclient = Nothing

            Case 4

                'TCP/IP Solver

                Dim tcpclient As New TCPSolverClient()

                Try
                    tcpclient.SolveFlowsheet(fobj)
                    For Each baseobj In fbag.SimulationObjects.Values
                        If baseobj.Calculated Then baseobj.LastUpdated = Date.Now
                    Next
                Catch ex As Exception
                    age = New AggregateException(ex.Message.ToString, ex)
                Finally
                    tcpclient.client.Close()
                End Try

                tcpclient = Nothing

        End Select

        'Frees GPU memory if enabled.

        If Settings.EnableGPUProcessing Then
            Settings.gpu.DisableMultithreading()
            Settings.gpu.FreeAll()
        End If

        'updates the display status of all objects in the calculation list.

        UpdateDisplayStatus(fobj, objstack.ToArray)

        'checks if exceptions were thrown during the calculation and displays them in the log window.

        If age Is Nothing Then

            fgui.ShowMessage(fgui.GetTranslatedString("FSfinishedsolvingok"), IFlowsheet.MessageType.Information)
            fgui.ShowMessage(fgui.GetTranslatedString("Runtime") & ": " & (Date.Now - d1).ToString("g"), IFlowsheet.MessageType.Information)

            'If Settings.StorePreviousSolutions Then

            '    'adds the current solution to the valid solution list.
            '    'the XML data is converted to a compressed byte array before being added to the collection.

            '    Dim stask As Task = Task.Factory.StartNew(Sub()
            '                                                  Try
            '                                                      Dim retbytes As MemoryStream = DWSIM.SimulationObjects.UnitOperations.Flowsheet.ReturnProcessData(fobj)
            '                                                      Using retbytes
            '                                                          Dim uncompressedbytes As Byte() = retbytes.ToArray
            '                                                          Using compressedstream As New MemoryStream()
            '                                                              Using gzs As New BufferedStream(New Compression.GZipStream(compressedstream, Compression.CompressionMode.Compress, True), 64 * 1024)
            '                                                                  gzs.Write(uncompressedbytes, 0, uncompressedbytes.Length)
            '                                                                  gzs.Close()
            '                                                                  Dim id As String = Date.Now.ToBinary.ToString
            '                                                                  If fobj.PreviousSolutions Is Nothing Then fobj.PreviousSolutions = New Dictionary(Of String, Flowsheet.FlowsheetSolution)
            '                                                                  fobj.PreviousSolutions.Add(id, New DWSIM.Flowsheet.FlowsheetSolution() With {.ID = id, .SaveDate = Date.Now, .Solution = compressedstream.ToArray})
            '                                                              End Using
            '                                                          End Using
            '                                                      End Using
            '                                                  Catch ex As Exception
            '                                                  End Try
            '                                              End Sub).ContinueWith(Sub(t)
            '                                                                        fobj.UpdateSolutionsList()
            '                                                                    End Sub, TaskContinuationOptions.ExecuteSynchronously)

            'End If

        Else

            Dim baseexception As Exception = Nothing

            fgui.ShowMessage(fgui.GetTranslatedString("FSfinishedsolvingerror"), IFlowsheet.MessageType.GeneralError)

            For Each ex In age.Flatten().InnerExceptions
                If TypeOf ex Is AggregateException Then
                    baseexception = ex.InnerException
                    For Each iex In DirectCast(ex, AggregateException).Flatten().InnerExceptions
                        While iex.InnerException IsNot Nothing
                            baseexception = iex.InnerException
                        End While
                        fgui.ShowMessage(baseexception.Message.ToString, IFlowsheet.MessageType.GeneralError)
                    Next
                Else
                    baseexception = ex
                    While ex.InnerException IsNot Nothing
                        baseexception = ex.InnerException
                    End While
                    fgui.ShowMessage(baseexception.Message.ToString, IFlowsheet.MessageType.GeneralError)
                End If
            Next

            age = Nothing

        End If

        'updates the flowsheet display information if the fobj is visible.

        'If fobj.Visible And fobj.MasterFlowsheet Is Nothing Then

        '    fobj.FormWatch.UpdateList()

        '    fobj.FormQueue.TextBox1.Clear()

        '    'For Each g As IGraphicObject In fobj.FormSurface.FlowsheetDesignSurface.drawingObjects
        '    '    If g.ObjectType = ObjectType.GO_MasterTable Then
        '    '        CType(g, MasterTableGraphic).Update(fobj)
        '    '    End If
        '    'Next

        '    If Not fobj.FormSpreadsheet Is Nothing Then
        '        If fobj.FormSpreadsheet.chkUpdate.Checked Then
        '            fobj.FormSpreadsheet.EvaluateAll()
        '            fobj.FormSpreadsheet.EvaluateAll()
        '        End If
        '    End If

        '    fobj.UpdateStatusLabel(preLab)

        '    If fobj.FormSurface.Timer2.Enabled = True Then fobj.FormSurface.Timer2.Stop()
        '    fobj.FormSurface.PictureBox3.Image = My.Resources.tick
        '    fobj.FormSurface.LabelTime.Text = ""

        '    'fobj.FormSurface.LabelSimultAdjInfo.Text = ""
        '    'fobj.FormSurface.PicSimultAdjust.Visible = False
        '    'fobj.FormSurface.LabelSimultAdjInfo.Visible = False
        '    'fobj.FormSurface.LabelSimultAdjustStatus.Visible = False

        '    If Not fobj.FormSurface.FlowsheetDesignSurface.SelectedObject Is Nothing Then Call fobj.FormSurface.UpdateSelectedObject()

        '    Application.DoEvents()

        'End If

        'fobj.ProcessScripts(Script.EventType.SolverFinished, Script.ObjectType.Solver)

        RaiseEvent FlowsheetCalculationFinished(fobj, New System.EventArgs(), Nothing)

    End Sub

    ''' <summary>
    ''' Calculate all objects in the Flowsheet.
    ''' </summary>
    ''' <param name="fobj">Flowsheet to be calculated (FormChild object)</param>
    ''' <remarks></remarks>
    Public Shared Sub CalculateAll(ByVal fobj As Object)

        SolveFlowsheet(fobj, Settings.SolverMode)

    End Sub

    ''' <summary>
    ''' Calculates a single object in the Flowsheet.
    ''' </summary>
    ''' <param name="fobj">Flowsheet where the object belongs to.</param>
    ''' <param name="ObjID">Unique Id of the object ("Name" or "GraphicObject.Name" properties). This is not the object's Flowsheet display name ("Tag" property or its GraphicObject object).</param>
    ''' <remarks></remarks>
    Public Shared Sub CalculateObject(ByVal fobj As Object, ByVal ObjID As String)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        If fbag.SimulationObjects.ContainsKey(ObjID) Then

            Dim baseobj = fbag.SimulationObjects(ObjID)

            Dim objargs As New CalculationArgs
            With objargs
                .Calculated = True
                .Name = baseobj.Name
                .ObjectType = baseobj.GraphicObject.ObjectType
                .Tag = baseobj.GraphicObject.Tag
            End With

            fqueue.CalculationQueue.Enqueue(objargs)

            SolveFlowsheet(fobj, Settings.SolverMode, , True)

        End If

    End Sub

    ''' <summary>
    ''' Calculates a single object in the Flowsheet.
    ''' </summary>
    ''' <param name="fobj">Flowsheet where the object belongs to.</param>
    ''' <param name="ObjID">Unique Id of the object ("Name" or "GraphicObject.Name" properties). This is not the object's Flowsheet display name ("Tag" property or its GraphicObject object).</param>
    ''' <remarks></remarks>
    Public Shared Sub CalculateObjectSync(ByVal fobj As Object, ByVal ObjID As String)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        If fbag.SimulationObjects.ContainsKey(ObjID) Then

            Dim baseobj = fbag.SimulationObjects(ObjID)

            If baseobj.GraphicObject.ObjectType = ObjectType.MaterialStream Then

                If baseobj.GraphicObject.InputConnectors(0).IsAttached = False Then
                    'add this stream to the calculator queue list
                    Dim objargs As New CalculationArgs
                    With objargs
                        .Calculated = True
                        .Name = baseobj.Name
                        .ObjectType = ObjectType.MaterialStream
                        .Tag = baseobj.GraphicObject.Tag
                    End With
                    If baseobj.IsSpecAttached = True And baseobj.SpecVarType = SpecVarType.Source Then
                        fbag.SimulationObjects(baseobj.AttachedSpecId).Calculate()
                    End If
                    fqueue.CalculationQueue.Enqueue(objargs)
                    ProcessQueueInternal(fobj)
                Else
                    If baseobj.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.ObjectType = ObjectType.OT_Recycle Then
                        'add this stream to the calculator queue list
                        Dim objargs As New CalculationArgs
                        With objargs
                            .Calculated = True
                            .Name = baseobj.Name
                            .ObjectType = ObjectType.MaterialStream
                            .Tag = baseobj.GraphicObject.Tag
                        End With
                        If baseobj.IsSpecAttached = True And baseobj.SpecVarType = SpecVarType.Source Then
                            fbag.SimulationObjects(baseobj.AttachedSpecId).Calculate()
                        End If
                        fqueue.CalculationQueue.Enqueue(objargs)
                        ProcessQueueInternal(fobj)
                    End If
                End If
            Else
                Dim unit As ISimulationObject = baseobj
                Dim objargs As New CalculationArgs
                With objargs
                    .Sender = "PropertyGrid"
                    .Calculated = True
                    .Name = unit.Name
                    .ObjectType = unit.GraphicObject.ObjectType
                    .Tag = unit.GraphicObject.Tag
                End With
                fqueue.CalculationQueue.Enqueue(objargs)
                ProcessQueueInternal(fobj)
            End If

        End If

    End Sub

    ''' <summary>
    ''' Calculates a single object in the Flowsheet.
    ''' </summary>
    ''' <param name="fobj">Flowsheet where the object belongs to.</param>
    ''' <param name="ObjID">Unique Id of the object ("Name" or "GraphicObject.Name" properties). This is not the object's Flowsheet display name ("Tag" property or its GraphicObject object).</param>
    ''' <param name="ct">The cancellation token, used to listen for calculation cancellation requests from the user.</param>
    ''' <remarks></remarks>
    Public Shared Sub CalculateObjectAsync(ByVal fobj As Object, ByVal ObjID As String, ByVal ct As CancellationToken)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        If fbag.SimulationObjects.ContainsKey(ObjID) Then

            Dim baseobj = fbag.SimulationObjects(ObjID)

            Dim objargs As New CalculationArgs
            With objargs
                .Sender = "PropertyGrid"
                .Calculated = True
                .Name = baseobj.Name
                .ObjectType = ObjectType.MaterialStream
                .Tag = baseobj.GraphicObject.Tag
            End With
            fqueue.CalculationQueue.Enqueue(objargs)

            Dim objl = GetSolvingList(fobj, True)

            Dim objstack As List(Of String) = objl(0)

            For Each o As String In objstack
                Dim obj = fbag.SimulationObjects(o)
                objargs = New CalculationArgs
                With objargs
                    .Sender = "FlowsheetSolver"
                    .Calculated = True
                    .Name = obj.Name
                    .ObjectType = obj.GraphicObject.ObjectType
                    .Tag = obj.GraphicObject.Tag
                    fqueue.CalculationQueue.Enqueue(objargs)
                End With
            Next

            For Each o In fbag.SimulationObjects.Values
                o.SetFlowsheet(fobj)
            Next

            ProcessQueueInternalAsync(fobj, ct)

        End If

    End Sub

    ''' <summary>
    ''' Simultaneous adjust solver routine.
    ''' </summary>
    ''' <param name="fobj">Flowsheet where the object belongs to.</param>
    ''' <remarks>Solves all marked Adjust objects in the flowsheet simultaneously using Netwon's method.</remarks>
    Private Shared Sub SolveSimultaneousAdjusts(ByVal fobj As Object)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        If Settings.SimultaneousAdjustEnabled Then

            Try

                Dim n As Integer = 0

                For Each adj As IAdjust In fbag.SimulationObjects.Values.Where(Function(a) TypeOf a Is IAdjust)
                    If adj.SimultaneousAdjust Then n += 1
                Next

                If n > 0 Then

                    n -= 1

                    Dim i As Integer = 0
                    Dim dfdx(n, n), dx(n), fx(n), x(n) As Double
                    Dim il_err_ant As Double = 10000000000.0
                    Dim il_err As Double = 10000000000.0
                    Dim ic As Integer

                    i = 0
                    For Each adj As IAdjust In fbag.SimulationObjects.Values.Where(Function(a) TypeOf a Is IAdjust)
                        If adj.SimultaneousAdjust Then
                            x(i) = GetMnpVarValue(fobj, adj)
                            i += 1
                        End If
                    Next

                    ic = 0
                    Do

                        fx = FunctionValueSync(fobj, x)

                        il_err_ant = il_err
                        il_err = 0
                        For i = 0 To x.Length - 1
                            il_err += (fx(i)) ^ 2
                        Next

                        'fobj.FormSurface.LabelSimultAdjInfo.Text = "Iteration #" & ic + 1 & ", NSSE: " & il_err

                        'Application.DoEvents()

                        If il_err < 0.0000000001 Then Exit Do

                        dfdx = FunctionGradientSync(fobj, x)

                        Dim success As Boolean
                        success = MathEx.SysLin.rsolve.rmatrixsolve(dfdx, fx, x.Length, dx)
                        If success Then
                            For i = 0 To x.Length - 1
                                dx(i) = -dx(i)
                                x(i) += dx(i)
                            Next
                        End If

                        ic += 1

                        If ic >= 100 Then Throw New Exception(fgui.GetTranslatedString("SADJMaxIterationsReached"))
                        If Double.IsNaN(il_err) Then Throw New Exception(fgui.GetTranslatedString("SADJGeneralError"))
                        If Math.Abs(MathEx.Common.AbsSum(dx)) < 0.000001 Then Exit Do

                    Loop

                End If

            Catch ex As Exception
                fgui.ShowMessage(fgui.GetTranslatedString("SADJGeneralError") & ": " & ex.Message.ToString, IFlowsheet.MessageType.GeneralError)
            Finally
                'fobj.FormSurface.LabelSimultAdjInfo.Text = ""
                'fobj.FormSurface.PicSimultAdjust.Visible = False
                'fobj.FormSurface.LabelSimultAdjInfo.Visible = False
                'fobj.FormSurface.LabelSimultAdjustStatus.Visible = False
            End Try

        End If

    End Sub

    ''' <summary>
    ''' Async simultaneous adjust solver routine.
    ''' </summary>
    ''' <param name="fobj">Flowsheet where the object belongs to.</param>
    ''' <param name="ct">The cancellation token, used to listen for calculation cancellation requests from the user.</param>
    ''' <remarks>Solves all marked Adjust objects in the flowsheet simultaneously using Netwon's method.</remarks>
    Private Shared Sub SolveSimultaneousAdjustsAsync(ByVal fobj As Object, ct As CancellationToken)

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
        Dim fqueue As IFlowsheetCalculationQueue = TryCast(fobj, IFlowsheetCalculationQueue)

        If Settings.SimultaneousAdjustEnabled Then

            'this is the cancellation token for background threads. it checks for calculator stop requests and passes the request to the tasks.

            Dim n As Integer = 0

            For Each adj As IAdjust In fbag.SimulationObjects.Values.Where(Function(a) TypeOf a Is IAdjust)
                If adj.SimultaneousAdjust Then n += 1
            Next

            If n > 0 Then

                'fobj.UIThread(Sub()
                '                  fobj.FormSurface.LabelSimultAdjInfo.Text = ""
                '                  fobj.FormSurface.PicSimultAdjust.Visible = True
                '                  fobj.FormSurface.LabelSimultAdjInfo.Visible = True
                '                  fobj.FormSurface.LabelSimultAdjustStatus.Visible = True

                '              End Sub)

                n -= 1

                Dim i As Integer = 0
                Dim dfdx(n, n), dx(n), fx(n), x(n) As Double
                Dim il_err_ant As Double = 10000000000.0
                Dim il_err As Double = 10000000000.0
                Dim ic As Integer

                i = 0
                For Each adj As IAdjust In fbag.SimulationObjects.Values.Where(Function(a) TypeOf a Is IAdjust)
                    If adj.SimultaneousAdjust Then
                        x(i) = GetMnpVarValue(fobj, adj)
                        i += 1
                    End If
                Next

                ic = 0
                Do

                    fx = FunctionValueAsync(fobj, x, ct)

                    il_err_ant = il_err
                    il_err = 0
                    For i = 0 To x.Length - 1
                        il_err += (fx(i)) ^ 2
                    Next

                    ' fobj.UIThread(Sub() fobj.FormSurface.LabelSimultAdjInfo.Text = "Iteration #" & ic + 1 & ", NSSE: " & il_err)

                    If il_err < 0.0000000001 Then Exit Do

                    dfdx = FunctionGradientAsync(fobj, x, ct)

                    Dim success As Boolean
                    success = MathEx.SysLin.rsolve.rmatrixsolve(dfdx, fx, x.Length, dx)
                    If success Then
                        For i = 0 To x.Length - 1
                            dx(i) = -dx(i)
                            x(i) += dx(i)
                        Next
                    End If

                    ic += 1

                    If ic >= 100 Then Throw New Exception(fgui.GetTranslatedString("SADJMaxIterationsReached"))
                    If Double.IsNaN(il_err) Then Throw New Exception(fgui.GetTranslatedString("SADJGeneralError"))
                    If Math.Abs(MathEx.Common.AbsSum(dx)) < 0.000001 Then Exit Do

                Loop

                'fobj.UIThread(Sub()
                '                  fobj.FormSurface.LabelSimultAdjInfo.Text = ""
                '                  fobj.FormSurface.PicSimultAdjust.Visible = False
                '                  fobj.FormSurface.LabelSimultAdjInfo.Visible = False
                '                  fobj.FormSurface.LabelSimultAdjustStatus.Visible = False
                '              End Sub)

            End If

        End If

    End Sub

    ''' <summary>
    ''' Function called by the simultaneous adjust solver. Retrieves the error function value for each adjust object.
    ''' </summary>
    ''' <param name="fobj">Flowsheet where the object belongs to.</param>
    ''' <param name="x"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function FunctionValueSync(ByVal fobj As Object, ByVal x() As Double) As Double()

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)
     
        Dim i As Integer = 0
        For Each adj As IAdjust In fbag.SimulationObjects.Values.Where(Function(a) TypeOf a Is IAdjust)
            If adj.SimultaneousAdjust Then
                SetMnpVarValue(x(i), fobj, adj)
                i += 1
            End If
        Next

        SolveFlowsheet(fobj, Settings.SolverMode, Nothing, False, True)

        Dim fx(x.Length - 1) As Double
        i = 0
        For Each adj As IAdjust In fbag.SimulationObjects.Values.Where(Function(a) TypeOf a Is IAdjust)
            If adj.SimultaneousAdjust Then
                If adj.Referenced Then
                    fx(i) = adj.AdjustValue + GetRefVarValue(fobj, adj) - GetCtlVarValue(fobj, adj)
                Else
                    fx(i) = adj.AdjustValue - GetCtlVarValue(fobj, adj)
                End If
                i += 1
            End If
        Next

        Return fx

    End Function

    ''' <summary>
    ''' Gradient function called by the simultaneous adjust solver. Retrieves the gradient of the error function value for each adjust object.
    ''' </summary>
    ''' <param name="fobj">Flowsheet where the object belongs to.</param>
    ''' <param name="x"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function FunctionGradientSync(ByVal fobj As Object, ByVal x() As Double) As Double(,)

        Dim epsilon As Double = 0.01

        Dim f2(), f3() As Double
        Dim g(x.Length - 1, x.Length - 1), x1(x.Length - 1), x2(x.Length - 1), x3(x.Length - 1), x4(x.Length - 1) As Double
        Dim i, j, k As Integer

        For i = 0 To x.Length - 1
            For j = 0 To x.Length - 1
                If i <> j Then
                    x2(j) = x(j)
                    x3(j) = x(j)
                Else
                    If x(j) <> 0.0# Then
                        x2(j) = x(j) * (1 + epsilon)
                        x3(j) = x(j) * (1 - epsilon)
                    Else
                        x2(j) = x(j) + epsilon
                        x3(j) = x(j)
                    End If
                End If
            Next
            f2 = FunctionValueSync(fobj, x2)
            f3 = FunctionValueSync(fobj, x3)
            For k = 0 To x.Length - 1
                g(k, i) = (f2(k) - f3(k)) / (x2(i) - x3(i))
            Next
        Next

        Return g

    End Function

    ''' <summary>
    ''' Function called asynchronously by the simultaneous adjust solver. Retrieves the error function value for each adjust object.
    ''' </summary>
    ''' <param name="fobj">Flowsheet where the object belongs to.</param>
    ''' <param name="x"></param>
    ''' <param name="ct">The cancellation token, used to listen for calculation cancellation requests from the user.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function FunctionValueAsync(ByVal fobj As Object, ByVal x() As Double, ct As CancellationToken) As Double()

        Dim fgui As IFlowsheetGUI = TryCast(fobj, IFlowsheetGUI)
        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        Dim i As Integer = 0
        For Each adj As IAdjust In fbag.SimulationObjects.Values.Where(Function(a) TypeOf a Is IAdjust)
            If adj.SimultaneousAdjust Then
                SetMnpVarValue(x(i), fobj, adj)
                i += 1
            End If
        Next

        SolveFlowsheet(fobj, Settings.SolverMode, Nothing, False, True)

        Dim fx(x.Length - 1) As Double
        i = 0
        For Each adj As IAdjust In fbag.SimulationObjects.Values.Where(Function(a) TypeOf a Is IAdjust)
            If adj.SimultaneousAdjust Then
                If adj.Referenced Then
                    fx(i) = adj.AdjustValue + GetRefVarValue(fobj, adj) - GetCtlVarValue(fobj, adj)
                Else
                    fx(i) = adj.AdjustValue - GetCtlVarValue(fobj, adj)
                End If
                i += 1
            End If
        Next

        Return fx

    End Function

    ''' <summary>
    ''' Gradient function called asynchronously by the simultaneous adjust solver. Retrieves the gradient of the error function value for each adjust object.
    ''' </summary>
    ''' <param name="fobj">Flowsheet where the object belongs to.</param>
    ''' <param name="x"></param>
    ''' <param name="ct">The cancellation token, used to listen for calculation cancellation requests from the user.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function FunctionGradientAsync(ByVal fobj As Object, ByVal x() As Double, ct As CancellationToken) As Double(,)

        Dim epsilon As Double = 0.01

        Dim f2(), f3() As Double
        Dim g(x.Length - 1, x.Length - 1), x1(x.Length - 1), x2(x.Length - 1), x3(x.Length - 1), x4(x.Length - 1) As Double
        Dim i, j, k As Integer

        For i = 0 To x.Length - 1
            For j = 0 To x.Length - 1
                If i <> j Then
                    x2(j) = x(j)
                    x3(j) = x(j)
                Else
                    If x(j) <> 0.0# Then
                        x2(j) = x(j) * (1 + epsilon)
                        x3(j) = x(j) * (1 - epsilon)
                    Else
                        x2(j) = x(j) + epsilon
                        x3(j) = x(j)
                    End If
                End If
            Next
            f2 = FunctionValueAsync(fobj, x2, ct)
            f3 = FunctionValueAsync(fobj, x3, ct)
            For k = 0 To x.Length - 1
                g(k, i) = (f2(k) - f3(k)) / (x2(i) - x3(i))
            Next
        Next

        Return g

    End Function

    ''' <summary>
    ''' Gets the controlled variable value for the selected adjust op.
    ''' </summary>
    ''' <param name="fobj"></param>
    ''' <param name="adj"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function GetCtlVarValue(ByVal fobj As Object, ByVal adj As IAdjust) As Double

        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        With adj.ControlledObjectData
            Return fbag.SimulationObjects(.ID).GetPropertyValue(.PropertyName)
        End With

    End Function

    ''' <summary>
    ''' Gets the manipulated variable value for the selected adjust op.
    ''' </summary>
    ''' <param name="fobj"></param>
    ''' <param name="adj"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function GetMnpVarValue(ByVal fobj As Object, ByVal adj As IAdjust) As Double

        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        With adj.ManipulatedObjectData
            Return fbag.SimulationObjects(.ID).GetPropertyValue(.PropertyName)
        End With

    End Function

    ''' <summary>
    ''' Sets the manipulated variable value for the selected adjust op.
    ''' </summary>
    ''' <param name="fobj"></param>
    ''' <param name="adj"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function SetMnpVarValue(ByVal val As Nullable(Of Double), ByVal fobj As Object, ByVal adj As IAdjust)

        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        With adj.ManipulatedObjectData
            fbag.SimulationObjects(.ID).SetPropertyValue(.PropertyName, val)
        End With

        Return 1

    End Function

    ''' <summary>
    ''' Gets the referenced variable value for the selected adjust op.
    ''' </summary>
    ''' <param name="fobj"></param>
    ''' <param name="adj"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function GetRefVarValue(ByVal fobj As Object, ByVal adj As IAdjust) As Double

        Dim fbag As IFlowsheetBag = TryCast(fobj, IFlowsheetBag)

        With adj.ManipulatedObjectData
            With adj.ControlledObjectData()
                Return fbag.SimulationObjects(.ID).GetPropertyValue(.Name)
            End With
        End With

    End Function

End Class