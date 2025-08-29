---
allowed-tools: Bash(git *), Read, TodoWrite
argument-hint: [feature_name]
description: Start implementing a feature by creating a new branch and initializing tasks from the feature plan
---

Start implementing feature: $ARGUMENTS

I'll help you begin work on this feature by:

1. **Reading the feature plan** from `.claude/features/$ARGUMENTS.md`
2. **Creating a new git branch** based on the feature name
3. **Setting up the development environment** 
4. **Initializing tasks** from the feature plan for organized development

Let me start by checking if the feature plan exists and reading its contents.

First, I'll check the current git status and create a new branch from main:

```bash
# Check current git status
git status

# Ensure we're on main branch
git checkout main

# Pull latest changes
git pull origin main

# Create and checkout new feature branch
git checkout -b "feature/$ARGUMENTS"

# Push the new branch to remote
git push -u origin "feature/$ARGUMENTS"
```

Now let me read the feature plan to understand the implementation requirements:

I'll read the feature plan from `.claude/features/$ARGUMENTS.md` and extract the implementation phases to create a structured task list.

Once I've analyzed the feature plan, I'll:
- Create todos for each implementation phase
- Identify which specialized agents should handle different components
- Set up the initial development workflow
- Provide clear next steps for implementation

This will give you a structured approach to implementing the feature with proper version control and task management in place.