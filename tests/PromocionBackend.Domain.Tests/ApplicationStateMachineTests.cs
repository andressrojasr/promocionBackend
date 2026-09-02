using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Domain.Tests;

public class ApplicationStateMachineTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(ApplicationStatus.Submitted, ReviewStages.Th, true, ApplicationStatus.ThApproved)]
    [InlineData(ApplicationStatus.Submitted, ReviewStages.Th, false, ApplicationStatus.ThRejected)]
    [InlineData(ApplicationStatus.ThApproved, ReviewStages.Cp, true, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.ThApproved, ReviewStages.Cp, false, ApplicationStatus.CpRejected)]
    [InlineData(ApplicationStatus.Appealed, ReviewStages.Ca, true, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Appealed, ReviewStages.Ca, false, ApplicationStatus.Rejected)]
    public void GetNextStatus_ValidTransitions(ApplicationStatus current, string stage, bool approved, ApplicationStatus expected)
    {
        var result = ApplicationStateMachine.GetNextStatus(current, stage, approved);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ApplicationStatus.Submitted, ReviewStages.Cp)]
    [InlineData(ApplicationStatus.Submitted, ReviewStages.Ca)]
    [InlineData(ApplicationStatus.ThApproved, ReviewStages.Th)]
    [InlineData(ApplicationStatus.Approved, ReviewStages.Cp)]
    [InlineData(ApplicationStatus.CpRejected, ReviewStages.Ca)]
    public void GetNextStatus_InvalidTransitions_ReturnNull(ApplicationStatus current, string stage)
    {
        Assert.Null(ApplicationStateMachine.GetNextStatus(current, stage, approved: true));
    }

    [Fact]
    public void GetEffectiveStatus_WithinAppealWindow_StaysCpRejected()
    {
        var cpDecisionAt = Now.AddDays(-2);

        var effective = ApplicationStateMachine.GetEffectiveStatus(ApplicationStatus.CpRejected, cpDecisionAt, Now);

        Assert.Equal(ApplicationStatus.CpRejected, effective);
    }

    [Fact]
    public void GetEffectiveStatus_ExactlyAtDeadline_StillAppealable()
    {
        var cpDecisionAt = Now.AddDays(-ApplicationStateMachine.AppealWindowDays);

        var effective = ApplicationStateMachine.GetEffectiveStatus(ApplicationStatus.CpRejected, cpDecisionAt, Now);

        Assert.Equal(ApplicationStatus.CpRejected, effective);
        Assert.True(ApplicationStateMachine.CanAppeal(ApplicationStatus.CpRejected, cpDecisionAt, Now));
    }

    [Fact]
    public void GetEffectiveStatus_AfterDeadline_BecomesRejected()
    {
        var cpDecisionAt = Now.AddDays(-ApplicationStateMachine.AppealWindowDays).AddSeconds(-1);

        var effective = ApplicationStateMachine.GetEffectiveStatus(ApplicationStatus.CpRejected, cpDecisionAt, Now);

        Assert.Equal(ApplicationStatus.Rejected, effective);
        Assert.False(ApplicationStateMachine.CanAppeal(ApplicationStatus.CpRejected, cpDecisionAt, Now));
    }

    [Fact]
    public void GetEffectiveStatus_OtherStatuses_Unchanged()
    {
        Assert.Equal(ApplicationStatus.Submitted,
            ApplicationStateMachine.GetEffectiveStatus(ApplicationStatus.Submitted, null, Now));
        Assert.Equal(ApplicationStatus.Approved,
            ApplicationStateMachine.GetEffectiveStatus(ApplicationStatus.Approved, Now.AddDays(-10), Now));
    }

    [Fact]
    public void GetAppealDeadline_OnlyForCpRejected()
    {
        var cpDecisionAt = Now;

        Assert.Equal(Now.AddDays(3), ApplicationStateMachine.GetAppealDeadline(ApplicationStatus.CpRejected, cpDecisionAt));
        Assert.Null(ApplicationStateMachine.GetAppealDeadline(ApplicationStatus.Submitted, cpDecisionAt));
        Assert.Null(ApplicationStateMachine.GetAppealDeadline(ApplicationStatus.CpRejected, null));
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
