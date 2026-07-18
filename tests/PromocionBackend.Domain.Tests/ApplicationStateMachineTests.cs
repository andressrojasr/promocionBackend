using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Domain.Tests;

public class ApplicationStateMachineTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(ApplicationStatuses.Submitted, ReviewStages.Th, true, ApplicationStatuses.ThApproved)]
    [InlineData(ApplicationStatuses.Submitted, ReviewStages.Th, false, ApplicationStatuses.ThRejected)]
    [InlineData(ApplicationStatuses.ThApproved, ReviewStages.Cp, true, ApplicationStatuses.Approved)]
    [InlineData(ApplicationStatuses.ThApproved, ReviewStages.Cp, false, ApplicationStatuses.CpRejected)]
    [InlineData(ApplicationStatuses.Appealed, ReviewStages.Ca, true, ApplicationStatuses.Approved)]
    [InlineData(ApplicationStatuses.Appealed, ReviewStages.Ca, false, ApplicationStatuses.Rejected)]
    public void GetNextStatus_ValidTransitions(string current, string stage, bool approved, string expected)
    {
        Assert.Equal(expected, ApplicationStateMachine.GetNextStatus(current, stage, approved));
    }

    [Theory]
    [InlineData(ApplicationStatuses.Submitted, ReviewStages.Cp)]
    [InlineData(ApplicationStatuses.Submitted, ReviewStages.Ca)]
    [InlineData(ApplicationStatuses.ThApproved, ReviewStages.Th)]
    [InlineData(ApplicationStatuses.Approved, ReviewStages.Cp)]
    [InlineData(ApplicationStatuses.CpRejected, ReviewStages.Ca)]
    public void GetNextStatus_InvalidTransitions_ReturnNull(string current, string stage)
    {
        Assert.Null(ApplicationStateMachine.GetNextStatus(current, stage, approved: true));
    }

    [Fact]
    public void GetEffectiveStatus_WithinAppealWindow_StaysCpRejected()
    {
        var cpDecisionAt = Now.AddDays(-2);

        var effective = ApplicationStateMachine.GetEffectiveStatus(ApplicationStatuses.CpRejected, cpDecisionAt, Now);

        Assert.Equal(ApplicationStatuses.CpRejected, effective);
    }

    [Fact]
    public void GetEffectiveStatus_ExactlyAtDeadline_StillAppealable()
    {
        var cpDecisionAt = Now.AddDays(-ApplicationStateMachine.AppealWindowDays);

        var effective = ApplicationStateMachine.GetEffectiveStatus(ApplicationStatuses.CpRejected, cpDecisionAt, Now);

        Assert.Equal(ApplicationStatuses.CpRejected, effective);
        Assert.True(ApplicationStateMachine.CanAppeal(ApplicationStatuses.CpRejected, cpDecisionAt, Now));
    }

    [Fact]
    public void GetEffectiveStatus_AfterDeadline_BecomesRejected()
    {
        var cpDecisionAt = Now.AddDays(-ApplicationStateMachine.AppealWindowDays).AddSeconds(-1);

        var effective = ApplicationStateMachine.GetEffectiveStatus(ApplicationStatuses.CpRejected, cpDecisionAt, Now);

        Assert.Equal(ApplicationStatuses.Rejected, effective);
        Assert.False(ApplicationStateMachine.CanAppeal(ApplicationStatuses.CpRejected, cpDecisionAt, Now));
    }

    [Fact]
    public void GetEffectiveStatus_OtherStatuses_Unchanged()
    {
        Assert.Equal(ApplicationStatuses.Submitted,
            ApplicationStateMachine.GetEffectiveStatus(ApplicationStatuses.Submitted, null, Now));
        Assert.Equal(ApplicationStatuses.Approved,
            ApplicationStateMachine.GetEffectiveStatus(ApplicationStatuses.Approved, Now.AddDays(-10), Now));
    }

    [Fact]
    public void GetAppealDeadline_OnlyForCpRejected()
    {
        var cpDecisionAt = Now;

        Assert.Equal(Now.AddDays(3), ApplicationStateMachine.GetAppealDeadline(ApplicationStatuses.CpRejected, cpDecisionAt));
        Assert.Null(ApplicationStateMachine.GetAppealDeadline(ApplicationStatuses.Submitted, cpDecisionAt));
        Assert.Null(ApplicationStateMachine.GetAppealDeadline(ApplicationStatuses.CpRejected, null));
    }

    [Theory]
    [InlineData(Roles.Th, ReviewStages.Th)]
    [InlineData(Roles.Cp, ReviewStages.Cp)]
    [InlineData(Roles.Ca, ReviewStages.Ca)]
    public void StageForRole_ReviewerRoles(string role, string expectedStage)
    {
        Assert.Equal(expectedStage, ApplicationStateMachine.StageForRole(role));
    }

    [Theory]
    [InlineData(Roles.Teacher)]
    [InlineData(Roles.Admin)]
    public void StageForRole_NonReviewerRoles_ReturnNull(string role)
    {
        Assert.Null(ApplicationStateMachine.StageForRole(role));
    }
}

public class PositionLadderTests
{
    [Theory]
    [InlineData(PositionLadder.Auxiliar1, PositionLadder.Auxiliar2)]
    [InlineData(PositionLadder.Auxiliar2, PositionLadder.Agregado1)]
    [InlineData(PositionLadder.Agregado1, PositionLadder.Agregado2)]
    [InlineData(PositionLadder.Agregado2, PositionLadder.Agregado3)]
    [InlineData(PositionLadder.Principal1, PositionLadder.Principal2)]
    [InlineData(PositionLadder.Principal2, PositionLadder.Principal3)]
    public void GetNextPosition_ValidTransitions(string from, string expected)
    {
        Assert.Equal(expected, PositionLadder.GetNextPosition(from));
    }

    [Theory]
    [InlineData(PositionLadder.Agregado3)]
    [InlineData(PositionLadder.Principal3)]
    [InlineData("POSICION_DESCONOCIDA")]
    public void GetNextPosition_NoTransition_ReturnsNull(string from)
    {
        Assert.Null(PositionLadder.GetNextPosition(from));
    }
}
