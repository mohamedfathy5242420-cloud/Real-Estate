import { defineConfig } from 'vitest/config';

// Keep Windows worker startup predictable while retaining every test file.
export default defineConfig({ test: { maxWorkers: 1, fileParallelism: false } });
