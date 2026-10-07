// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "xUnit1047:Avoid using TheoryDataRow arguments that might not be serializable", Justification = "<Pending>", Scope = "member", Target = "~F:Respira.Clinical.Domain.Test.Models.ExpressionTest.FailPath_BranchExpressionNotNumeric")]
[assembly: SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable", Justification = "<Pending>", Scope = "member", Target = "~M:Respira.Clinical.Domain.Test.Models.ExpressionTest.TernaryExpressionTest_NotNumericIfTrue_Fail(Respira.Clinical.Domain.Models.Expression,Respira.Clinical.Domain.Models.Expression)")]
[assembly: SuppressMessage("Usage", "xUnit1047:Avoid using TheoryDataRow arguments that might not be serializable", Justification = "<Pending>", Scope = "member", Target = "~F:Respira.Clinical.Domain.Test.Models.RangeTest.HappyPath_IsInRange")]
[assembly: SuppressMessage("Usage", "xUnit1047:Avoid using TheoryDataRow arguments that might not be serializable", Justification = "<Pending>", Scope = "member", Target = "~F:Respira.Clinical.Domain.Test.Models.RangeTest.HappyPath_IsRangeOverlapped")]
[assembly: SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable", Justification = "<Pending>", Scope = "member", Target = "~M:Respira.Clinical.Domain.Test.Models.RangeTest.IsInRange_Success(Respira.Clinical.Domain.Models.Range,System.Decimal,System.Boolean)")]
[assembly: SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable", Justification = "<Pending>", Scope = "member", Target = "~M:Respira.Clinical.Domain.Test.Models.RangeTest.IsRangeOverlapped_Success(Respira.Clinical.Domain.Models.Range,Respira.Clinical.Domain.Models.Range,System.Boolean)")]
[assembly: SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable", Justification = "<Pending>", Scope = "member", Target = "~M:Respira.Clinical.Domain.Test.Models.RangeTest.IsRangeContained_Success(Respira.Clinical.Domain.Models.Range,Respira.Clinical.Domain.Models.Range,System.Boolean)")]
