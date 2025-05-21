
Public Class AlphanumComparator
        Implements IComparer(Of String)

        Public Function Compare(x As String, y As String) As Integer Implements IComparer(Of String).Compare

            ' [1] Validate the arguments.
            Dim s1 As String = x
            If s1 = Nothing Then
                Return 0
            End If

            Dim s2 As String = y
            If s2 = Nothing Then
                Return 0
            End If

            Dim len1 As Integer = s1.Length
            Dim len2 As Integer = s2.Length
            Dim marker1 As Integer = 0
            Dim marker2 As Integer = 0

            ' [2] Loop over both Strings.
            While marker1 < len1 And marker2 < len2

                ' [3] Get Chars.
                Dim ch1 As Char = s1(marker1)
                Dim ch2 As Char = s2(marker2)

                Dim space1(len1) As Char
                Dim loc1 As Integer = 0
                Dim space2(len2) As Char
                Dim loc2 As Integer = 0

                ' [4] Collect digits for String one.
                Do
                    space1(loc1) = ch1
                    loc1 += 1
                    marker1 += 1

                    If marker1 < len1 Then
                        ch1 = s1(marker1)
                    Else
                        Exit Do
                    End If
                Loop While Char.IsDigit(ch1) = Char.IsDigit(space1(0))

                ' [5] Collect digits for String two.
                Do
                    space2(loc2) = ch2
                    loc2 += 1
                    marker2 += 1

                    If marker2 < len2 Then
                        ch2 = s2(marker2)
                    Else
                        Exit Do
                    End If
                Loop While Char.IsDigit(ch2) = Char.IsDigit(space2(0))

                ' [6] Convert to Strings.
                Dim str1 = New String(space1)
                Dim str2 = New String(space2)

                ' [7] Parse Strings into Integers.
                Dim result As Integer
                If Char.IsDigit(space1(0)) And Char.IsDigit(space2(0)) Then
                    Dim thisNumericChunk = Integer.Parse(str1)
                    Dim thatNumericChunk = Integer.Parse(str2)
                    result = thisNumericChunk.CompareTo(thatNumericChunk)
                Else
                    result = str1.CompareTo(str2)
                End If

                ' [8] Return result if not equal.
                If Not result = 0 Then
                    Return result
                End If
            End While

            ' [9] Compare lengths.
            Return len1 - len2
        End Function
    End Class