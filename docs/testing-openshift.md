# Testing OpenShift

OpenShift can be a bit of a pain. Under Windows, OpenShift Local (the development version) is hard coded to use port 80/443, which is very inconvenient if you have IIS installed locally. The official workaround is to change the ports that IIS uses. There nothing like miniKube or K3s for OpenShift, and OpenShift is incredibly heavy (needs at least 9GB of RAM and 20+ GB of disk space). So, testing OpenShift is currently a manual process.

Setup:
- Create a VM with your Linux based OS of choice - I Use Ubuntu LTS, server edition. Make sure the VM has around 16GB of RAM and plenty of disk space. I allocated the VM 16 threads and the VM still wanted more resources.
- Ensure that nested virtualization is enabled on BOTH the host machine and the VM (Hyper-V requires this to be enabled per VM).
- Ensure you have a valid RedHat developer account and download "Red Hat OpenShift Local" (requires a developer login).
- Install "Red Hat OpenShift Local" onto your VM.
- Login into the OpenShift cluster as a cluster admin.

## Actually Testing

Deploy `install-prod.yaml` from [releases](https://github.com/Contrast-Security-OSS/agent-operator/releases).

```bash
# Install the production manifests.
oc apply -f install-prod.yaml

# Wait for the cluster to converge.
watch oc -n contrast-agent-operator get pods

# Check the logs for any problems.
oc -n contrast-agent-operator logs deployment/contrast-agent-operator -f
```

Then we can deploy examples and make sure everything is peachy.

```bash
# Install the standard manifests that should work in every cluster.
# Note that PHP will fail-restart-crash unless the OS cluster can access our private container registry.
oc apply -k ./manifests/examples/dev

# Optionally, install all our manifests that are used for automated testing.
oc apply -k ./manifests/examples/testing

# Install the OpenShift only examples (e.g. DeploymentConfig).
oc apply -k ./manifests/examples/openshift
```

OpenShift will likely not deploy everything because the cluster will run out of memory requests. In which case, you will need to increase the amount of RAM in the OS cluster, or remove some workloads.
