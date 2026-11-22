CC = gcc
SRC = webpify.c
TARGET_O2 = webpify_o2.exe
TARGET_O3 = webpify_o3.exe

COMMON_FLAGS = -Wall -Wextra -Wconversion -static
LIBS = -lwebp -lsharpyuv
CLEAN_FILES = $(TARGET_O2) $(TARGET_O3)

.PHONY: all o2 o3 clean

all: o2 o3

o2: $(TARGET_O2)

o3: $(TARGET_O3)

$(TARGET_O2): $(SRC) wuffs-v0.4.c
	@$(CC) -O2 $(COMMON_FLAGS) $(SRC) -o $@ $(LIBS)
	@printf "$@ make complete.\n"

$(TARGET_O3): $(SRC) wuffs-v0.4.c
	@$(CC) -O3 $(COMMON_FLAGS) $(SRC) -o $@ $(LIBS)
	@printf "$@ make complete.\n"

clean:
	@rm -f $(CLEAN_FILES)
	@echo "Clean up done."