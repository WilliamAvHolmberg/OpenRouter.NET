# Publishing Guide for @openrouter-dotnet/react

This guide explains how to build, test, and publish the React SDK to npm.

## 📦 Prerequisites

1. **Node.js** (v18 or higher)
2. **npm** account with access to `@openrouter-dotnet` scope
3. **Git** configured with your credentials

## 🚀 Quick Start

```bash
# Navigate to the package directory
cd packages/react-sdk

# Install dependencies
npm install

# Build the package
npm run build

# Test locally
npm pack

# Publish to npm
npm publish
```

## 🔧 Detailed Steps

### 1. Install Dependencies

```bash
cd packages/react-sdk
npm install
```

This installs all required dependencies including:
- TypeScript compiler
- Rollup bundler
- Type definitions for React
- Build tools

### 2. Build the Package

```bash
npm run build
```

This command:
- Compiles TypeScript to JavaScript
- Generates both CommonJS and ES modules
- Creates TypeScript declaration files
- Generates source maps
- Outputs to `dist/` directory

**Build Output:**
```
dist/
├── index.js          # CommonJS bundle
├── index.esm.js      # ES modules bundle
├── index.d.ts        # TypeScript declarations
├── index.d.ts.map    # Declaration source map
├── index.js.map      # Source map for CommonJS
└── index.esm.js.map  # Source map for ES modules
```

### 3. Test Locally

Before publishing, test the package locally:

```bash
# Create a tarball
npm pack

# This creates: openrouter-dotnet-react-1.0.0.tgz
```

**Test in another project:**
```bash
# In a test project
npm install /path/to/openrouter-dotnet-react-1.0.0.tgz

# Or install from local path
npm install file:../packages/react-sdk
```

### 4. Verify Package Contents

```bash
# Check what will be published
npm pack --dry-run

# Or inspect the tarball
tar -tzf openrouter-dotnet-react-1.0.0.tgz
```

### 5. Publish to npm

#### First Time Publishing

```bash
# Login to npm (if not already logged in)
npm login

# Publish the package
npm publish
```

#### Subsequent Releases

```bash
# Update version
npm version patch  # or minor, major

# Publish
npm publish
```

## 📋 Pre-Publish Checklist

Before publishing, ensure:

- [ ] **Version updated** in `package.json`
- [ ] **README.md** is up to date
- [ ] **CHANGELOG.md** updated (if you have one)
- [ ] **Tests pass** (when you add them)
- [ ] **Build succeeds** without errors
- [ ] **Package contents** are correct
- [ ] **Dependencies** are properly configured

## 🔄 Version Management

### Semantic Versioning

- **Patch** (1.0.0 → 1.0.1): Bug fixes
- **Minor** (1.0.0 → 1.1.0): New features, backward compatible
- **Major** (1.0.0 → 2.0.0): Breaking changes

### Update Version

```bash
# Patch version (bug fixes)
npm version patch

# Minor version (new features)
npm version minor

# Major version (breaking changes)
npm version major
```

## 🧪 Testing the Package

### Local Testing

1. **Create a test project:**
```bash
mkdir test-react-sdk
cd test-react-sdk
npm init -y
npm install react react-dom
```

2. **Install your package:**
```bash
npm install file:../packages/react-sdk
```

3. **Create a test component:**
```tsx
// test.tsx
import React from 'react';
import { useOpenRouterChat } from '@openrouter-dotnet/react';

function TestApp() {
  const { state, actions } = useOpenRouterChat({
    baseUrl: 'https://your-api.com'
  });

  return (
    <div>
      <h1>Test App</h1>
      <button onClick={() => actions.sendMessage('Hello!')}>
        Send Message
      </button>
      <div>
        {state.messages.map(msg => (
          <div key={msg.id}>{msg.role}: {msg.blocks.length} blocks</div>
        ))}
      </div>
    </div>
  );
}

export default TestApp;
```

4. **Test the build:**
```bash
npx tsc test.tsx --jsx react
```

### Automated Testing

When you add tests:

```bash
# Run tests
npm test

# Run tests in watch mode
npm run test:watch

# Run tests with coverage
npm run test:coverage
```

## 🚨 Troubleshooting

### Common Issues

#### 1. Build Errors
```bash
# Clear node_modules and reinstall
rm -rf node_modules package-lock.json
npm install

# Clear build cache
npm run clean
npm run build
```

#### 2. TypeScript Errors
```bash
# Check TypeScript version
npx tsc --version

# Update TypeScript
npm install typescript@latest
```

#### 3. Rollup Errors
```bash
# Check Rollup version
npx rollup --version

# Update Rollup
npm install rollup@latest
```

#### 4. Publishing Errors

**Scope not found:**
```bash
# Check if you have access to the scope
npm whoami
npm access list packages @openrouter-dotnet
```

**Package already exists:**
```bash
# Check if version already exists
npm view @openrouter-dotnet/react versions --json
```

## 📚 Package Configuration

### Key Files

- **`package.json`** - Package metadata and dependencies
- **`tsconfig.json`** - TypeScript configuration
- **`rollup.config.js`** - Build configuration
- **`.npmignore`** - Files to exclude from package

### Important Settings

```json
{
  "name": "@openrouter-dotnet/react",
  "type": "module",
  "main": "dist/index.js",
  "module": "dist/index.esm.js",
  "types": "dist/index.d.ts",
  "files": ["dist", "README.md", "LICENSE"],
  "peerDependencies": {
    "react": ">=18.0.0",
    "react-dom": ">=18.0.0"
  }
}
```

## 🔐 Security & Access

### NPM Scopes

To publish to `@openrouter-dotnet` scope:

1. **Create the scope** (if not exists):
```bash
npm org create openrouter-dotnet
```

2. **Add team members:**
```bash
npm org add openrouter-dotnet <username>
```

3. **Set access level:**
```bash
npm access set <access-level> @openrouter-dotnet/react
```

### Authentication

```bash
# Login to npm
npm login

# Check authentication
npm whoami

# Logout
npm logout
```

## 📈 Monitoring

### Package Analytics

- **npmjs.com** - View download stats
- **npm trends** - Compare with other packages
- **GitHub** - Track issues and contributions

### Health Checks

```bash
# Check package health
npm audit

# Check for outdated dependencies
npm outdated

# Check package size
npm pack --dry-run | wc -c
```

## 🎯 Best Practices

### 1. Version Management
- Use semantic versioning
- Update CHANGELOG.md
- Tag releases in Git

### 2. Quality Assurance
- Test before publishing
- Use TypeScript strict mode
- Include comprehensive documentation

### 3. Security
- Keep dependencies updated
- Use `npm audit` regularly
- Don't commit sensitive data

### 4. Documentation
- Keep README.md updated
- Include usage examples
- Document breaking changes

## 🚀 CI/CD (Future)

When you're ready for automation:

```yaml
# .github/workflows/publish.yml
name: Publish Package
on:
  push:
    tags: ['v*']
jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '18'
          registry-url: 'https://registry.npmjs.org'
      - run: npm ci
      - run: npm run build
      - run: npm publish
        env:
          NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}
```

## 📞 Support

If you encounter issues:

1. Check this guide first
2. Search existing issues
3. Create a new issue with:
   - Error messages
   - Steps to reproduce
   - Environment details

---

**Happy Publishing! 🎉**
