using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectPlanController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ProjectPlanController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // POST: api/ProjectPlan/project/1/generate
    [HttpPost("project/{projectId:long}/generate")]
    public async Task<IActionResult> GenerateProjectPlan(
        long projectId,
        [FromBody] GenerateProjectPlanRequest request)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                return NotFound(new
                {
                    message = "Project not found."
                });
            }

            var stages = await _context.ProjectStages
                .Where(s => s.ProjectId == projectId)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            if (stages.Count == 0)
            {
                return BadRequest(new
                {
                    message =
                        "Project stages have not been initialized. Initialize project stages before generating the execution plan."
                });
            }

            var projectServices = await _context.ProjectServices
                .Where(ps => ps.ProjectId == projectId)
                .Include(ps => ps.Service)
                .Include(ps => ps.ServiceOption)
                .ToListAsync();

            if (projectServices.Count == 0)
            {
                return BadRequest(new
                {
                    message = "The project has no selected services."
                });
            }

            var existingTasks = await _context.ProjectTasks
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();

            var newTasks = new List<ProjectTask>();

            var designStage = stages.FirstOrDefault(
                s => s.StageCode == "DESIGN");

            var customerReviewStage = stages.FirstOrDefault(
                s => s.StageCode == "CUSTOMER_REVIEW");

            var planningStage = stages.FirstOrDefault(
                s => s.StageCode == "PLANNING");

            var executionStage = stages.FirstOrDefault(
                s => s.StageCode == "EXECUTION");

            var inspectionStage = stages.FirstOrDefault(
                s => s.StageCode == "INSPECTION");

            var handoverStage = stages.FirstOrDefault(
                s => s.StageCode == "HANDOVER");


            // =========================================================
            // INTERIOR / DESIGN SERVICES
            // =========================================================

            foreach (var service in projectServices)
            {
                var optionCode =
                    service.ServiceOption?.OptionCode?.ToUpperInvariant();

                var serviceName =
                    service.Service.ServiceName;

                if (optionCode == "OUR_SERVICE" &&
                    IsDesignService(serviceName))
                {
                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        designStage?.ProjectStageId,
                        service.ProjectServiceId,
                        "Space Planning",
                        "Develop the initial interior space planning based on the confirmed customer requirements.",
                        request.CreatedByUserId,
                        "HIGH");

                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        designStage?.ProjectStageId,
                        service.ProjectServiceId,
                        "Design Development",
                        "Develop the detailed interior design based on the approved space planning.",
                        request.CreatedByUserId,
                        "HIGH");

                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        designStage?.ProjectStageId,
                        service.ProjectServiceId,
                        "3D Presentation",
                        "Prepare 3D views and presentation material for customer review.",
                        request.CreatedByUserId,
                        "MEDIUM");

                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        customerReviewStage?.ProjectStageId,
                        service.ProjectServiceId,
                        "Customer Design Approval",
                        "Present the design to the customer and record the design approval.",
                        request.CreatedByUserId,
                        "HIGH");
                }


                // =====================================================
                // SUPPLY SERVICES
                // =====================================================

                if (optionCode == "SUPPLY")
                {
                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        planningStage?.ProjectStageId,
                        service.ProjectServiceId,
                        $"{serviceName} - Selection & Confirmation",
                        "Confirm the required material/product specification with the customer.",
                        request.CreatedByUserId,
                        "HIGH");

                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        planningStage?.ProjectStageId,
                        service.ProjectServiceId,
                        $"{serviceName} - Quantity Calculation",
                        "Calculate the required quantity based on approved design, measurements and site requirements.",
                        request.CreatedByUserId,
                        "HIGH");

                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        planningStage?.ProjectStageId,
                        service.ProjectServiceId,
                        $"{serviceName} - Procurement",
                        "Arrange procurement of the approved materials/products.",
                        request.CreatedByUserId,
                        "HIGH");

                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        executionStage?.ProjectStageId,
                        service.ProjectServiceId,
                        $"{serviceName} - Material Delivery",
                        "Coordinate delivery of the materials/products to the project site.",
                        request.CreatedByUserId,
                        "HIGH");
                }


                // =====================================================
                // RESOURCE SERVICES
                // =====================================================

                if (optionCode == "RESOURCE")
                {
                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        planningStage?.ProjectStageId,
                        service.ProjectServiceId,
                        $"{serviceName} - Resource Identification",
                        "Identify suitable external resource/vendor for the required work.",
                        request.CreatedByUserId,
                        "HIGH");

                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        planningStage?.ProjectStageId,
                        service.ProjectServiceId,
                        $"{serviceName} - Resource Assignment",
                        "Confirm and assign the selected resource/vendor to the project service.",
                        request.CreatedByUserId,
                        "HIGH");

                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        executionStage?.ProjectStageId,
                        service.ProjectServiceId,
                        $"{serviceName} - Execution",
                        "Carry out the required work at the project site.",
                        request.CreatedByUserId,
                        "HIGH");

                    AddTaskIfMissing(
                        newTasks,
                        existingTasks,
                        projectId,
                        inspectionStage?.ProjectStageId,
                        service.ProjectServiceId,
                        $"{serviceName} - Final Inspection",
                        "Inspect the completed work and record the final quality result.",
                        request.CreatedByUserId,
                        "HIGH");
                }
            }


            // =========================================================
            // PROJECT HANDOVER
            // These are project-level tasks and therefore do not belong
            // to an individual ProjectService.
            // =========================================================

            if (handoverStage != null)
            {
                AddTaskIfMissing(
                    newTasks,
                    existingTasks,
                    projectId,
                    handoverStage.ProjectStageId,
                    null,
                    "Final Customer Inspection",
                    "Conduct the final inspection of the completed project with the customer and confirm that the project is ready for handover.",
                    request.CreatedByUserId,
                    "HIGH");

                AddTaskIfMissing(
                    newTasks,
                    existingTasks,
                    projectId,
                    handoverStage.ProjectStageId,
                    null,
                    "Snag / Punch List",
                    "Record all outstanding defects, corrections or customer observations identified during the final inspection.",
                    request.CreatedByUserId,
                    "HIGH");

                AddTaskIfMissing(
                    newTasks,
                    existingTasks,
                    projectId,
                    handoverStage.ProjectStageId,
                    null,
                    "Snag Rectification",
                    "Complete the rectification of all approved snag and punch-list items.",
                    request.CreatedByUserId,
                    "HIGH");

                AddTaskIfMissing(
                    newTasks,
                    existingTasks,
                    projectId,
                    handoverStage.ProjectStageId,
                    null,
                    "Final Verification",
                    "Verify that all snag items have been rectified and that the completed project is ready for customer handover.",
                    request.CreatedByUserId,
                    "HIGH");

                AddTaskIfMissing(
                    newTasks,
                    existingTasks,
                    projectId,
                    handoverStage.ProjectStageId,
                    null,
                    "Documents / Warranty Handover",
                    "Prepare and hand over applicable drawings, documents, warranties, certificates and other project handover records.",
                    request.CreatedByUserId,
                    "MEDIUM");

                AddTaskIfMissing(
                    newTasks,
                    existingTasks,
                    projectId,
                    handoverStage.ProjectStageId,
                    null,
                    "Final Payment Clearance",
                    "Confirm completion of applicable final billing and payment clearance before customer handover.",
                    request.CreatedByUserId,
                    "HIGH");

                AddTaskIfMissing(
                    newTasks,
                    existingTasks,
                    projectId,
                    handoverStage.ProjectStageId,
                    null,
                    "Customer Handover Acceptance",
                    "Obtain the customer's final handover acceptance and close the project.",
                    request.CreatedByUserId,
                    "HIGH");
            }


            // =========================================================
            // SAVE NEW TASKS
            // =========================================================

            if (newTasks.Count > 0)
            {
                _context.ProjectTasks.AddRange(newTasks);

                await _context.SaveChangesAsync();
            }


            // =========================================================
            // RELOAD ALL TASKS
            // This includes both existing and newly-created tasks.
            // =========================================================

            var allTasks = await _context.ProjectTasks
                .Where(t => t.ProjectId == projectId)
                .OrderBy(t => t.ProjectTaskId)
                .ToListAsync();


            // =========================================================
            // ASSIGN DEPENDENCIES
            // =========================================================

            var dependencyChanges =
                AssignDependencies(allTasks);


            if (dependencyChanges > 0)
            {
                await _context.SaveChangesAsync();
            }


            await transaction.CommitAsync();


            // =========================================================
            // RETURN RESULT
            // =========================================================

            return Ok(new
            {
                message =
                    "Project execution plan generated successfully.",

                projectId,

                existingTaskCount =
                    existingTasks.Count,

                newTaskCount =
                    newTasks.Count,

                dependencyChanges,

                totalTaskCount =
                    allTasks.Count,

                tasks = allTasks
                    .OrderBy(t => t.ProjectTaskId)
                    .Select(t => new
                    {
                        t.ProjectTaskId,
                        t.ProjectStageId,
                        t.ProjectServiceId,
                        t.TaskName,
                        t.Status,
                        t.Priority,
                        t.DependsOnProjectTaskId
                    })
            });
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }


    // =============================================================
    // DESIGN SERVICE IDENTIFICATION
    // =============================================================

    private static bool IsDesignService(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return false;
        }

        var name = serviceName.ToUpperInvariant();

        return name.Contains("DESIGN") ||
               name.Contains("ARCHITECT") ||
               name.Contains("PLANNING");
    }


    // =============================================================
    // ADD TASK ONLY IF IT DOES NOT ALREADY EXIST
    // =============================================================

    private static void AddTaskIfMissing(
        List<ProjectTask> newTasks,
        List<ProjectTask> existingTasks,
        long projectId,
        long? projectStageId,
        long? projectServiceId,
        string taskName,
        string description,
        long? assignedToUserId,
        string priority)
    {
        var existsInDatabase = existingTasks.Any(t =>
            t.ProjectServiceId == projectServiceId &&
            t.TaskName == taskName);

        var existsInNewTasks = newTasks.Any(t =>
            t.ProjectServiceId == projectServiceId &&
            t.TaskName == taskName);

        if (existsInDatabase || existsInNewTasks)
        {
            return;
        }

        newTasks.Add(new ProjectTask
        {
            ProjectId = projectId,
            ProjectStageId = projectStageId,
            ProjectServiceId = projectServiceId,

            TaskName = taskName,
            Description = description,

            AssignedToUserId = assignedToUserId,

            Status = "PENDING",
            Priority = priority,

            ProgressPercent = 0,

            CreatedAt = DateTime.Now
        });
    }


    // =============================================================
    // ASSIGN TASK DEPENDENCIES
    // =============================================================

    private static int AssignDependencies(
        List<ProjectTask> tasks)
    {
        var changes = 0;

        // ---------------------------------------------------------
        // Each ProjectService has its own workflow chain.
        // ---------------------------------------------------------
        var serviceGroups = tasks
            .Where(t => t.ProjectServiceId.HasValue)
            .GroupBy(t => t.ProjectServiceId!.Value);

        foreach (var group in serviceGroups)
        {
            var serviceTasks = group
                .OrderBy(t => GetWorkflowOrder(t.TaskName))
                .ThenBy(t => t.ProjectTaskId)
                .ToList();

            ProjectTask? previousTask = null;

            foreach (var task in serviceTasks)
            {
                if (task.DependsOnProjectTaskId.HasValue)
                {
                    previousTask = task;
                    continue;
                }

                if (previousTask == null)
                {
                    previousTask = task;
                    continue;
                }

                if (previousTask.ProjectTaskId == task.ProjectTaskId)
                {
                    previousTask = task;
                    continue;
                }

                task.DependsOnProjectTaskId = previousTask.ProjectTaskId;
                changes++;
                previousTask = task;
            }
        }

        // ---------------------------------------------------------
        // Project-level Handover workflow.
        // Handover tasks are chained independently from service
        // workflows because ProjectServiceId is NULL.
        // ---------------------------------------------------------
        var handoverTasks = tasks
            .Where(t => !t.ProjectServiceId.HasValue &&
                        GetWorkflowOrder(t.TaskName) >= 300 &&
                        GetWorkflowOrder(t.TaskName) < 400)
            .OrderBy(t => GetWorkflowOrder(t.TaskName))
            .ThenBy(t => t.ProjectTaskId)
            .ToList();

        ProjectTask? previousHandoverTask = null;

        foreach (var task in handoverTasks)
        {
            if (task.DependsOnProjectTaskId.HasValue)
            {
                previousHandoverTask = task;
                continue;
            }

            // The first Handover task is intentionally left without a
            // dependency here. Starting Handover must also be protected
            // by the ProjectTasksController/business rule that verifies
            // all applicable ProjectServices are completed.
            if (previousHandoverTask == null)
            {
                previousHandoverTask = task;
                continue;
            }

            if (previousHandoverTask.ProjectTaskId == task.ProjectTaskId)
            {
                previousHandoverTask = task;
                continue;
            }

            task.DependsOnProjectTaskId = previousHandoverTask.ProjectTaskId;
            changes++;
            previousHandoverTask = task;
        }

        return changes;
    }


    // =============================================================
    // WORKFLOW ORDER
    // =============================================================

    private static int GetWorkflowOrder(
        string taskName)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            return 9999;
        }

        var name = taskName.ToUpperInvariant();


        // Requirement / design
        if (name == "REQUIREMENT CONFIRMATION")
            return 10;

        if (name == "SPACE PLANNING")
            return 20;

        if (name == "DESIGN DEVELOPMENT")
            return 30;

        if (name == "3D PRESENTATION")
            return 40;

        if (name == "CUSTOMER DESIGN APPROVAL")
            return 50;


        // Supply
        if (name.EndsWith(
            " - SELECTION & CONFIRMATION"))
            return 100;

        if (name.EndsWith(
            " - QUANTITY CALCULATION"))
            return 110;

        if (name.EndsWith(
            " - PROCUREMENT"))
            return 120;

        if (name.EndsWith(
            " - MATERIAL DELIVERY"))
            return 130;


        // Resource
        if (name.EndsWith(
            " - RESOURCE IDENTIFICATION"))
            return 200;

        if (name.EndsWith(
            " - RESOURCE ASSIGNMENT"))
            return 210;

        if (name.EndsWith(
            " - EXECUTION"))
            return 220;

        if (name.EndsWith(
            " - FINAL INSPECTION"))
            return 230;


        // Project Handover
        if (name == "FINAL CUSTOMER INSPECTION")
            return 300;

        if (name == "SNAG / PUNCH LIST")
            return 310;

        if (name == "SNAG RECTIFICATION")
            return 320;

        if (name == "FINAL VERIFICATION")
            return 330;

        if (name == "DOCUMENTS / WARRANTY HANDOVER")
            return 340;

        if (name == "FINAL PAYMENT CLEARANCE")
            return 350;

        if (name == "CUSTOMER HANDOVER ACCEPTANCE")
            return 360;


        return 9999;
    }
}