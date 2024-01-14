IMAGE_NAME = memorial
IMAGE_TAG = v0.2
DOCKERFILE_DIR = .
WORK_DIR = .
PROJECT = metaverse
REPO_NAME = registry.hub.docker.com/ferdinandsu
APP_NAME ?= $(IMAGE_NAME)
SH_APP_NAME = $(APP_NAME)_sh
DOCKER = docker
DEFAULT_RUN_OPTIONS = --cap-add=SYS_PTRACE --shm-size=1024m

.PHONY: default install restart start stop logs uninstall deploy sh status

default:
	$(DOCKER) build -f $(DOCKERFILE_DIR)/Dockerfile -t $(IMAGE_NAME):$(IMAGE_TAG) $(WORK_DIR)

install:
	$(DOCKER) run $(RUN_OPTIONS) -p 80:80 -tid --restart=on-failure --name $(APP_NAME) $(IMAGE_NAME):$(IMAGE_TAG)

restart:
	$(DOCKER) restart $(APP_NAME)

start:
	$(DOCKER) start $(APP_NAME)

stop:
	$(DOCKER) stop $(APP_NAME)

logs:
	$(DOCKER) logs -f $(APP_NAME)

uninstall:
	$(DOCKER) rm -f $(APP_NAME)

tag:
	$(DOCKER) tag $(IMAGE_NAME):$(IMAGE_TAG) $(REPO_NAME)/$(IMAGE_NAME):$(IMAGE_TAG)

publish: tag
	$(DOCKER) push $(REPO_NAME)/$(IMAGE_NAME):$(IMAGE_TAG)

sh:
	$(DOCKER) run $(RUN_OPTIONS) -tid --restart=on-failure --name $(SH_APP_NAME) --entrypoint=/bin/sh $(IMAGE_NAME):$(IMAGE_TAG)

runsh: sh
	$(DOCKER) attach $(SH_APP_NAME)

cleansh:
	$(DOCKER) rm -f $(SH_APP_NAME)

status:
	$(DOCKER) ps -a | grep $(IMAGE_NAME)