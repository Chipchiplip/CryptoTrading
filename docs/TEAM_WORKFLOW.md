# Team Workflow & Git Strategy

## Team Structure

**Lead**: Đặng Công Vũ Hoàng  
**Members**: 5 Fullstack Developers

| Member | Primary Feature | Branch |
|--------|----------------|--------|
| Nhật An | Authentication System | `feature/auth-system` |
| Hữu Triết | Market Data System | `feature/market-data` |
| Trung Hiếu | Watchlist & Portfolio | `feature/watchlist-portfolio` |
| Vũ Hoàng | Trading System | `feature/trading-system` |
| Đăng Khoa | Payment & Subscription | `feature/payments-subscription` |

## Branching Strategy

```
main (protected)
  ├── feature/auth-system
  ├── feature/market-data
  ├── feature/watchlist-portfolio
  ├── feature/trading-system
  └── feature/payments-subscription
```

### Branch Rules
- **main**: Production-ready code, requires PR approval
- **feature/***: Feature development branches
- **hotfix/***: Emergency production fixes
- **release/***: Release preparation branches

## Daily Workflow

### 1. Morning (9:00 AM) - Daily Standup
Each member answers:
1. What did I complete yesterday?
2. What am I working on today?
3. Any blockers?

Duration: **15 minutes max**

### 2. Development Workflow

#### Step 1: Create/Update Feature Branch
```bash
# First time
git checkout main
git pull origin main
git checkout -b feature/your-feature

# Subsequent days
git checkout feature/your-feature
git pull origin feature/your-feature
git merge main  # Keep your branch up to date
```

#### Step 2: Make Changes
```bash
# Make your code changes
# Run tests locally
dotnet test

# Commit with clear message
git add .
git commit -m "feat: add user login endpoint"

# Push to remote
git push origin feature/your-feature
```

#### Step 3: Create Pull Request
1. Go to GitHub/GitLab
2. Create PR from `feature/your-feature` → `main`
3. Fill in PR template:
   - Description of changes
   - Related issue/ticket
   - Screenshots (if UI changes)
   - Checklist completed

#### Step 4: Code Review
- Lead reviews all PRs
- Address review comments
- Update PR with fixes
- Get approval

#### Step 5: Merge
- Lead merges approved PRs
- Delete feature branch after merge
- Pull latest main before starting next feature

## Commit Message Convention

### Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Formatting, missing semicolons
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks

### Examples
```bash
git commit -m "feat(auth): add JWT token refresh"
git commit -m "fix(orders): handle concurrent order placement"
git commit -m "docs: update API documentation"
git commit -m "test(market): add unit tests for price service"
```

## Code Review Guidelines

### For Reviewers
- ✅ Check code quality and readability
- ✅ Verify tests are included
- ✅ Ensure no secrets are committed
- ✅ Check for security vulnerabilities
- ✅ Verify API documentation is updated
- ⏱️ Review within 24 hours

### For Authors
- ✅ Self-review before requesting review
- ✅ Ensure all tests pass
- ✅ Update documentation
- ✅ Keep PRs small and focused
- ✅ Respond to comments within 24 hours

## Testing Requirements

### Before Committing
```bash
# Run all tests
dotnet test

# Check code formatting
dotnet format --verify-no-changes

# Run linter (if configured)
dotnet build /warnaserror
```

### Before Creating PR
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] No linter warnings
- [ ] Code coverage > 80%

## Conflict Resolution

### Merge Conflicts
```bash
# Update your branch with latest main
git checkout feature/your-feature
git merge main

# Resolve conflicts in VS Code
# After resolving:
git add .
git commit -m "merge: resolve conflicts with main"
git push origin feature/your-feature
```

### Getting Help
1. Try to resolve yourself (Google, docs)
2. Ask team in Slack
3. Schedule pairing session with lead
4. Document solution for future reference

## Definition of Done (DoD)

A feature is "Done" when:

- [ ] ✅ Code is written and follows standards
- [ ] ✅ Unit tests pass (coverage > 80%)
- [ ] ✅ Integration tests pass
- [ ] ✅ Manual testing completed
- [ ] ✅ Code reviewed and approved
- [ ] ✅ Documentation updated
- [ ] ✅ API endpoints documented in Swagger
- [ ] ✅ No security vulnerabilities
- [ ] ✅ Performance acceptable
- [ ] ✅ Merged to main

## Sprint Planning

### Sprint Duration: 1 week (Monday - Friday)

### Monday (Sprint Planning)
- Review last sprint
- Plan current sprint tasks
- Assign story points
- Set sprint goals

### Daily (Standup)
- 9:00 AM - 15 minutes
- What's done, what's next, blockers

### Friday (Sprint Review & Retro)
#### Review (2 PM)
- Demo completed features
- Show working software
- Get stakeholder feedback

#### Retrospective (3 PM)
- What went well?
- What could be improved?
- Action items for next sprint

## Communication Channels

| Purpose | Channel | Response Time |
|---------|---------|---------------|
| Urgent bugs | Phone/SMS | Immediate |
| Quick questions | Slack DM | 1 hour |
| Team discussions | Slack #team | 2 hours |
| Code reviews | GitHub PR | 24 hours |
| Documentation | Confluence/Wiki | As needed |

## Best Practices

### Code Style
- Follow C# naming conventions
- Use meaningful variable names
- Keep methods small (<50 lines)
- Add XML comments for public APIs
- Use async/await properly

### Git Hygiene
- Commit early and often
- Write meaningful commit messages
- Keep commits atomic
- Don't commit secrets
- Don't commit generated files

### Security
- Never commit API keys or passwords
- Use environment variables for secrets
- Review code for SQL injection
- Validate all user inputs
- Follow OWASP guidelines

### Performance
- Profile before optimizing
- Use caching appropriately
- Optimize database queries
- Monitor API response times
- Set performance budgets

## Emergency Procedures

### Production Bug
1. Create hotfix branch: `hotfix/critical-bug-fix`
2. Fix bug with minimal changes
3. Add test to prevent regression
4. Fast-track review (< 2 hours)
5. Merge to main
6. Deploy immediately
7. Post-mortem within 24 hours

### Rollback
```bash
# Revert last commit on main
git revert HEAD
git push origin main

# Or revert specific commit
git revert <commit-hash>
```

## Tools

### Required
- Git (version control)
- VS Code or Visual Studio 2022
- Docker Desktop
- Postman or Insomnia (API testing)
- SSMS or Azure Data Studio

### Recommended
- GitKraken or SourceTree (Git GUI)
- Redis Commander (Redis UI)
- Hangfire Dashboard (background jobs)
- Application Insights (monitoring)

## Success Metrics

### Individual
- PRs merged per week: 3-5
- Code review turnaround: < 24h
- Test coverage: > 80%
- Bug rate: < 5% of features

### Team
- Sprint velocity (story points)
- Cycle time (feature → production)
- Bug escape rate
- Customer satisfaction

---

**Remember**: We're a team. Help each other, share knowledge, and ship quality code! 🚀

**Questions?** Ask in Slack or schedule a 1:1 with the lead.

