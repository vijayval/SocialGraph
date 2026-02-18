# Azure Front Door Setup Guide
## Complete Step-by-Step Guide for Beginners

This guide will help you set up Azure Front Door from scratch to route traffic to your API hosted on Azure App Service.

---

## Prerequisites

Before you start, you need:
- An Azure account with an active subscription
- An existing Azure App Service (your API already deployed)
- Basic understanding of web URLs

---

## Part 1: Create Azure Front Door

### Step 1: Open Azure Portal
1. Go to [https://portal.azure.com](https://portal.azure.com)
2. Sign in with your Azure account credentials
3. Wait for the Azure Portal dashboard to load

### Step 2: Search for Azure Front Door
1. At the top of the page, you'll see a **search bar** that says "Search resources, services, and docs"
2. Click on the search bar
3. Type: `Front Door`
4. In the dropdown list, click on **"Front Door and CDN profiles"**

### Step 3: Start Creating a New Front Door
1. On the "Front Door and CDN profiles" page, click the **"+ Create"** button (blue button at the top left)
2. You'll see a page titled "Compare offerings"
3. Look for the box labeled **"Azure Front Door"**
4. Under "Select", choose **"Custom create"** (not Quick create)
5. Click the blue **"Continue"** button at the bottom

### Step 4: Fill in Basic Information
On the "Basics" tab, fill in these fields:

1. **Subscription**: Select your Azure subscription from the dropdown
2. **Resource group**: 
   - Click "Create new" if you don't have one
   - Enter a name like: `rg-stunsy-dev`
   - OR select an existing resource group
3. **Name**: Enter a unique name for your Front Door
   - Example: `stunsy-afd-dev`
   - Note: This is an internal name, not the public URL
4. **Tier**: Select **"Premium"** (for security features)
5. **Endpoint name**: Enter a name for your endpoint
   - Example: `stunsy-socialgraph-dev`
   - This will be part of your public URL
6. **Origin type**: Select **"App services"**
7. **Origin host name**: Select your App Service from the dropdown
   - Example: `stunsy-socialgraph-api-staging.azurewebsites.net`

### Step 5: Review and Create
1. Scroll down and click **"Review + create"** button
2. Wait for validation to complete (should show green checkmark)
3. Click the blue **"Create"** button
4. Wait 5-10 minutes for Azure to create your Front Door
5. When done, click **"Go to resource"**

---

## Part 2: Configure Origin Group and Health Probes

### Step 6: Open Front Door Manager
1. In your Front Door resource page, look at the left sidebar
2. Under the **"Settings"** section, click **"Front Door manager"**
3. You'll see a visual diagram showing:
   - Endpoint (your entry point)
   - Origin group (your backend)
   - Route (connection between them)

### Step 7: Configure Origin Group Health Probes
1. In the left sidebar, under **"Settings"**, click **"Origin groups"**
2. You'll see a list with `default-origin-group`
3. Click on **"default-origin-group"** to open it
4. You'll see your origin (App Service) listed
5. Under "Health probes" section, check these settings:
   - **Status**: Check the box for "Enable health probes" ✓
   - **Path**: Change from `/` to **`/health`**
   - **Protocol**: Change from HTTP to **HTTPS**
   - **Probe method**: Keep as **HEAD**
   - **Interval (in seconds)**: Keep as **100**
6. Click the blue **"Update"** button at the bottom
7. Wait a few seconds for it to save

---

## Part 3: Create and Configure Route (CRITICAL STEP)

This is the most important part! Without a route, Front Door won't work.

### Step 8: Create a Route
1. In the left sidebar, under **"Settings"**, click **"Front Door manager"**
2. You should see a visual diagram
3. Look for a **"+ Add a route"** button or link
4. Click **"+ Add a route"**

### Step 9: Configure Route Settings

Fill in the route configuration form:

#### Basic Settings:
- **Name**: Enter `default-route`
- **Endpoint**: Select your endpoint from the dropdown
  - Example: `stunsy-socialgraph-dev-erd0bpgzfgghcwbm.z03.azurefd.net`
- **Enable route**: Make sure the checkbox is checked ✓

#### Domains:
- **Domains**: Select your Front Door endpoint domain from the dropdown
  - It should match your endpoint name

#### Patterns to match:
- **Patterns to match**: Enter `/*`
  - This means "match all URLs"
  - Delete any other patterns like `/path` if they exist
  - Only keep `/*`

#### Protocols:
- **Accepted protocols**: Select **"HTTPS only"** OR **"HTTP and HTTPS"**
- **Redirect**: If you selected "HTTP and HTTPS", check the box ✓ for:
  - "Redirect all traffic to use HTTPS"

#### Origin Group (MOST IMPORTANT):
- **Origin group**: Select **"default-origin-group"** from the dropdown
  - This connects your route to your App Service!
- **Origin path**: Leave this field **empty** (or enter `/` if required)
- **Forwarding protocol**: Select **"HTTPS only"**
  - This ensures Front Door uses HTTPS when connecting to your App Service

#### Caching:
- **Enable caching**: Leave **unchecked** (disabled) for APIs

#### Rules:
- Leave this section as default (no rules needed for basic setup)

### Step 10: Save the Route
1. Scroll to the bottom of the form
2. Click the blue **"Add"** or **"Update"** button
3. Wait for the confirmation message
4. **IMPORTANT**: Wait 5-10 minutes for Azure to apply the changes

---

## Part 4: Test Your Setup

### Step 11: Get Your Front Door URL
1. Go back to your Front Door overview page
2. Look for "Endpoint hostname" or click on "Endpoints" in the left sidebar
3. Copy your Front Door URL, it will look like:
   - `https://your-endpoint-name-xxxxx.z03.azurefd.net`
   - Example: `https://stunsy-socialgraph-dev-erd0bpgzfgghcwbm.z03.azurefd.net`

### Step 12: Test with Command Line (PowerShell or Azure Cloud Shell)

#### Option A: Using PowerShell
1. Open PowerShell on your computer
2. Run this command (replace with your URL):
```powershell
Invoke-WebRequest -Uri "https://your-front-door-url.azurefd.net/health" -Method Get -UseBasicParsing
```

#### Option B: Using Azure Cloud Shell
1. In Azure Portal, click the **">_"** icon at the top (Cloud Shell)
2. Select **Bash**
3. Run this command (replace with your URL):
```bash
curl -v https://your-front-door-url.azurefd.net/health
```

### Step 13: Verify Success
You should see:
- **Status Code**: 200 OK
- **Response**: "Healthy" (or your API's health response)
- **Headers**: Should show your App Service domain in the response

If you see **"Page not found"** or 404 error:
- Go back to Part 3 and verify your route is configured correctly
- Make sure the Origin group is selected in the route
- Wait another 5-10 minutes for changes to propagate

---

## Part 5: Troubleshooting Common Issues

### Issue 1: "Page not found" Error

**Problem**: You see a 404 error with Azure's error page

**Solution**:
1. Go to Front Door → Settings → Front Door manager
2. Check if you have a **Route** created
3. Click on your route and verify:
   - ✓ "Enable route" is checked
   - ✓ Pattern is `/*`
   - ✓ Origin group is selected (not empty!)
   - ✓ Forwarding protocol is "HTTPS only"
4. Click Update and wait 5-10 minutes

### Issue 2: Health Probe Failing

**Problem**: Origin shows as "unhealthy" in Origin groups

**Solution**:
1. Verify your App Service has a `/health` endpoint that returns 200 OK
2. Check health probe settings:
   - Path: `/health`
   - Protocol: HTTPS
   - Method: HEAD
3. Test your App Service directly first:
```powershell
Invoke-WebRequest -Uri "https://your-app-service.azurewebsites.net/health"
```

### Issue 3: Changes Not Taking Effect

**Problem**: You made changes but still see old behavior

**Solution**:
- Azure Front Door changes take 5-20 minutes to propagate globally
- Try **"Purge cache"** button in Front Door overview
- Wait at least 10 minutes after any configuration change
- Test from a different browser or in incognito/private mode

### Issue 4: SSL/Certificate Errors

**Problem**: HTTPS connection fails or shows certificate errors

**Solution**:
- Azure Front Door provides automatic SSL certificates
- If using a custom domain, you need to add it and verify it first
- For the default `.azurefd.net` domain, SSL should work automatically

---

## Key Concepts Explained

### What is Azure Front Door?
Think of it as a smart traffic director for your website/API. It:
- Sits in front of your App Service
- Provides a global entry point (one URL)
- Routes traffic to your backend
- Provides security, caching, and load balancing

### What is an Endpoint?
- The public URL people/apps use to access your API
- Example: `https://stunsy-socialgraph-dev-xxxxx.z03.azurefd.net`

### What is an Origin?
- Your actual App Service where your API code runs
- Example: `https://stunsy-socialgraph-api-staging.azurewebsites.net`

### What is an Origin Group?
- A container for one or more origins
- Can have multiple App Services for load balancing
- Includes health probe settings

### What is a Route?
- **THE MOST CRITICAL PART**
- Connects your Endpoint to your Origin Group
- Tells Front Door: "When someone visits X, forward to Y"
- Without a route, Front Door doesn't know where to send traffic!

### What are Health Probes?
- Automatic checks Front Door makes to your API
- Verifies your App Service is healthy and responding
- If health check fails, Front Door won't send traffic there

---

## Summary: The Three Required Components

For Azure Front Door to work, you MUST have all three:

1. **Endpoint** (created automatically)
   - Your public Front Door URL

2. **Origin + Origin Group** (created automatically)
   - Your App Service backend
   - Health probe settings

3. **Route** (YOU MUST CREATE THIS!)
   - Connects endpoint to origin group
   - Pattern: `/*`
   - Must select origin group
   - Forwarding: HTTPS only

**Remember**: If you skip creating the route or don't configure it properly with an origin group, Front Door will return "Page not found" errors!

---

## Final Checklist

Before considering your setup complete, verify:

- [ ] Front Door resource is created
- [ ] Endpoint exists and has a URL
- [ ] Origin group contains your App Service
- [ ] Health probe is configured (Path: `/health`, Protocol: HTTPS)
- [ ] **Route is created and enabled**
- [ ] Route has pattern `/*`
- [ ] **Route has origin group selected** (most critical!)
- [ ] Route forwarding protocol is "HTTPS only"
- [ ] Waited 5-10 minutes after last change
- [ ] Tested with curl/PowerShell and got 200 OK response

---

## Next Steps

Once your Front Door is working:

1. **Add Custom Domain** (optional):
   - Go to Settings → Domains
   - Add your custom domain (e.g., `api.yourdomain.com`)
   - Verify domain ownership
   - Update route to include custom domain

2. **Enable WAF (Web Application Firewall)**:
   - Go to Settings → WAF policy
   - Create or associate a WAF policy
   - Configure security rules

3. **Configure Caching** (for non-API scenarios):
   - Edit your route
   - Enable caching with appropriate rules

4. **Monitor and Alerts**:
   - Go to Monitoring → Metrics
   - Set up alerts for health check failures
   - Monitor request counts and latency

---

## Need Help?

If you're still having issues:
1. Check Azure Service Health for any ongoing issues
2. Review Azure Front Door documentation: [https://learn.microsoft.com/azure/frontdoor/](https://learn.microsoft.com/azure/frontdoor/)
3. Contact Azure Support through the Azure Portal

---

**Document Version**: 1.0  
**Last Updated**: February 5, 2026  
**Author**: Social Graph API Team
