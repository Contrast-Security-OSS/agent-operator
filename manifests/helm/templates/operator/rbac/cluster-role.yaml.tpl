{{ if ne .Values.operator.enabled false }}
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: contrast-agent-operator-service-role
  labels:
    app.kubernetes.io/name: operator
    app.kubernetes.io/part-of: contrast-agent-operator
rules:
- apiGroups:
  - apps
  resources:
  - daemonsets
  - deployments
  - statefulsets
  verbs:
  - get
  - list
  - watch
  - patch
- apiGroups:
  - ""
  resources:
  - pods
  verbs:
  - get
  - list
  - watch
  - patch
- apiGroups:
  - ""
  resources:
  - secrets
  verbs:
  - get
  - list
  - watch
  - create
  - update
  - patch
  - delete
- apiGroups:
  - admissionregistration.k8s.io
  resources:
  - mutatingwebhookconfigurations
  verbs:
  - get
  - list
  - watch
  - patch
- apiGroups:
  - agents.contrastsecurity.com
  resources:
  - agentconfigurations
  - agentconnections
  - agentinjectors
  verbs:
  - get
  - list
  - watch
  - create
  - update
  - patch
  - delete
- apiGroups:
  - agents.contrastsecurity.com
  resources:
  - clusteragentconfigurations
  - clusteragentconnections
  verbs:
  - get
  - list
  - watch
- apiGroups:
  - apps.openshift.io
  resources:
  - deploymentconfigs
  verbs:
  - get
  - list
  - watch
  - patch
- apiGroups:
  - dynatrace.com
  resources:
  - dynakubes
  verbs:
  - get
  - list
  - watch
- apiGroups:
  - argoproj.io
  resources:
  - rollouts
  verbs:
  - get
  - list
  - watch
  - patch
- apiGroups:
  - ""
  resources:
  - events
  verbs:
  - get
  - list
  - create
  - update
- apiGroups:
  - coordination.k8s.io
  resources:
  - leases
  verbs:
  - get
  - list
  - watch
  - create
  - update
  - patch
  - delete
- apiGroups:
  - apps
  resources:
  - daemonsets/status
  verbs:
  - get
  - update
  - patch
- apiGroups:
  - apps
  resources:
  - deployments/status
  verbs:
  - get
  - update
  - patch
- apiGroups:
  - ""
  resources:
  - pods/status
  verbs:
  - get
  - update
  - patch
- apiGroups:
  - apps
  resources:
  - statefulsets/status
  verbs:
  - get
  - update
  - patch
{{ end }}
